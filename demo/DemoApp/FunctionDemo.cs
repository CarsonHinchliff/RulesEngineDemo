// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using RulesEngine.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static FastExpressionCompiler.ExpressionCompiler;

namespace DemoApp
{
    // 静态工具类示例
    public static class StringUtils
    {
        // 静态方法：检查字符串是否为邮箱格式
        public static bool IsEmail(string input)
        {
            if (string.IsNullOrEmpty(input))
                return false;
            return Regex.IsMatch(input, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        }

        // 静态方法：字符串脱敏（隐藏中间字符）
        public static string MaskString(string input, int keepStart = 2, int keepEnd = 2)
        {
            if (string.IsNullOrEmpty(input) || input.Length <= keepStart + keepEnd)
                return input;
            return input.Substring(0, keepStart) +
                   new string('*', input.Length - keepStart - keepEnd) +
                   input.Substring(input.Length - keepEnd);
        }

        public static string MethodOnOutput(UserOutput output)
        {
            return Guid.NewGuid().ToString();
        }
    }
    public class FunctionDemo
    {
        public void Run()
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "FunctionRules.json", SearchOption.AllDirectories);
            if (files == null || files.Length == 0)
            {
                throw new Exception("Rules not found.");
            }

            var fileData = File.ReadAllText(files[0]);
            var Workflows = JsonSerializer.Deserialize<List<Workflow>>(fileData);
            // 初始化 RulesEngine 配置
            var reSettings = new ReSettings {
                // 注册类型别名：键为别名，值为类型全名（命名空间+类名）
                CustomTypes = new Type[] { typeof(StringUtils) }
            };

            // 初始化 RulesEngine 引擎
            var rulesEngine = new RulesEngine.RulesEngine(Workflows.ToArray(), reSettings);

            // 准备输入数据
            var input = new UserInput {
                Email = "test@example.com",
                Phone = "13800138000",
                Properties = new Dictionary<string, object> {
                    { "Key1", 1 },
                    { "Key2", "abc" }
                }
            };

            // 执行规则
            var result = rulesEngine.ExecuteAllRulesAsync(
                "UserValidationWorkflow", // 工作流名称
                input,                    // 输入数据
                new UserOutput()          // 输出对象（可空，默认返回 RuleResultTree）
            ).Result;

            // 处理结果
            if (result.All(r => r.IsSuccess))
            {
                var output = result.First().ActionResult.Output as UserOutput;
                Console.WriteLine($"验证结果：{output.IsValid}");
                Console.WriteLine($"脱敏手机号：{output.MaskedPhone}"); // 输出：138****8000
            }
        }
    }

    // 定义输入模型
    public class UserInput
    {
        public string Email { get; set; }
        public string Phone { get; set; }

        public Dictionary<string, object> Properties { get; set; } = new();
    }

    // 定义输出模型
    public class UserOutput
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
        public string MaskedPhone { get; set; }

        public Dictionary<string, object> Properties { get; set; } = new();
    }
}
