﻿using System;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using Topshelf;

namespace WinServiceWrapper
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var name = ConfigurationManager.AppSettings["Name"];
			var target = ConfigurationManager.AppSettings["TargetExecutable"];

			if (string.IsNullOrWhiteSpace(name))
				throw new Exception("You must name your service in the App.Config file, key = \"Name\"");
			if (string.IsNullOrWhiteSpace(target))
				throw new Exception("You must provide a target executable to wrap, key= \"TargetExecutable\"");

			var safeName = MakeSafe(name);
			var description = ConfigurationManager.AppSettings["Description"];
			var startArgs = ConfigurationManager.AppSettings["StartCommand"];
			var stopArgs = ConfigurationManager.AppSettings["StopCommand"];
			var pauseArgs = ConfigurationManager.AppSettings["PauseCommand"];
			var continueArgs = ConfigurationManager.AppSettings["ContinueCommand"];

			var stdOutLog = ConfigurationManager.AppSettings["StdOutLog"];
			var stdErrLog = ConfigurationManager.AppSettings["StdErrLog"];

		// Dummy version of ourself -- just sit and wait
			if (args.FirstIs("waitForPid"))
			{
				var ppid = int.Parse(args[1]);
				Process.GetProcessById(ppid).WaitForExit();
				return;
			}

			// hack around TopShelf:
			if (args.FirstIs("pause"))
			{
				TryPauseService(safeName);
				return;
			}
			if (args.FirstIs("continue"))
			{
				TryContinueService(safeName);
				return;
			}

			HostFactory.Run(x =>
			{
				x.Service<WrapperService>(s =>
				{
					s.ConstructUsing(hostSettings => new WrapperService(name, target, startArgs, pauseArgs, continueArgs, stopArgs, stdOutLog, stdErrLog));

					s.WhenStarted(tc => tc.Start());
					s.WhenStopped(tc => tc.Stop());
					s.WhenPaused(tc => tc.Pause());
					s.WhenContinued(tc => tc.Continue());

				});
				x.RunAsLocalSystem();

				x.EnablePauseAndContinue();
				x.EnableServiceRecovery(sr => sr.RestartService(0));

				x.SetDisplayName(name);
				x.SetServiceName(safeName);
				x.SetDescription(description);
			});
		}

		static void TryContinueService(string serviceName)
		{
			using (var service = new ServiceController(serviceName))
			{
				service.Continue();
				service.WaitForStatus(ServiceControllerStatus.Running);
			}
		}

		static void TryPauseService(string serviceName)
		{
			using (var service = new ServiceController(serviceName))
			{
				service.Pause();
				service.WaitForStatus(ServiceControllerStatus.Paused);
			}
		}

		static string MakeSafe(string name)
		{
			var sb = new StringBuilder();
			foreach (char c in name.Where(char.IsLetterOrDigit))
			{
				sb.Append(c);
			}
			return sb.ToString();
		}
	}

	public static class Ext
	{
		public static bool FirstIs(this string[] args, string target)
		{
			return string.Equals(args.FirstOrDefault(), target, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
