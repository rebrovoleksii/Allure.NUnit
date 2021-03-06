﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Allure.Commons.Helpers
{
    internal static class BeforeAfterFixturesHelper
    {
        internal static Dictionary<MethodType, string> GetTypeOfCurrentMethodInTest()
        {
            var dict = new Dictionary<MethodType, string>();
            var methods = GetCallStackMethods().ToList();
            if (AllureLifecycle.Instance.Config.Allure.DebugMode)
            {
                var sb = new StringBuilder();
                methods.ForEach(m => sb.AppendLine(m.Name));
                Logger.LogInProgress(
                    $"Test ID {TestExecutionContext.CurrentContext.CurrentTest.Id}, Thread ID {Thread.CurrentThread.ManagedThreadId}, call stack at this moment\n {sb}");
                sb.Clear();
            }

            var method = methods.FirstOrDefault(sMethod =>
            {
                try
                {
                    _ = sMethod.CustomAttributes;
                }
                catch (Exception)
                {
                    return false;
                }

                var result = sMethod.DeclaringType != typeof(AllureReport) &&
                    sMethod.GetCustomAttributes().Any(
                        attr =>
                            attr is SetUpAttribute || attr is OneTimeSetUpAttribute ||
                            attr is TearDownAttribute ||
                            attr is OneTimeTearDownAttribute);
                return result;
            });
            if (method == null)
            {
                dict.Add(MethodType.TestBody, "");
                return dict;
            }

            var methodName = method.Name;
            var attrs = method.GetCustomAttributes();
            var methodType = MethodType.TestBody;
            foreach (var attribute in attrs)
                switch (attribute)
                {
                    case SetUpAttribute _:
                        methodType = MethodType.Setup;
                        break;
                    case OneTimeSetUpAttribute _:
                        methodType = MethodType.OneTimeSetup;
                        break;
                    case TearDownAttribute _:
                        methodType = MethodType.Teardown;
                        break;
                    case OneTimeTearDownAttribute _:
                        methodType = MethodType.OneTimeTearDown;
                        break;
                }
            dict.Add(methodType, methodName);
            return dict;
        }

        private static IEnumerable<MethodBase> GetCallStackMethods()
        {
            var count = 1;
            var stackTrace = new StackTrace();
            var method = stackTrace.GetFrame(1)?.GetMethod();
            do
            {
                yield return method;
                count++;
                method = stackTrace.GetFrame(count)?.GetMethod();
            } while (method != null);
        }

        internal enum MethodType
        {
            Setup,
            Teardown,
            OneTimeSetup,
            OneTimeTearDown,
            TestBody
        }
    }
}