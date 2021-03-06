﻿using System.Diagnostics;
using System.IO;
using NUnit.Framework;

namespace WinServiceWrapper.Integration.Tests
{
	[TestFixture]
    public class IntegrationTests_ExternalStop
    {
		string _fileName;
		Process _process;

		[TestFixtureSetUp]
		public void run_service_lifetime ()
		{
			_fileName = @"C:\Temp\dummyout.txt";
			if (File.Exists(_fileName)) File.Delete(_fileName);
			Call("WinServiceWrapper.exe", "install start");
			
			Call("WinServiceWrapper.exe", "pause");
			Call("WinServiceWrapper.exe", "continue");

			Call("WinServiceWrapper.exe", "stop uninstall");
		}

		[TestFixtureTearDown]
		public void cleanup ()
		{
			if (File.Exists(_fileName)) File.Delete(_fileName);
		}

		[Test]
		public void startup_passes_args_to_real_process ()
		{
			Assert.That(File.ReadAllText(_fileName), Contains.Substring("start, args"));
		}

		[Test]
		public void pause_calls_another_instance_with_arguments ()
		{
			Assert.That(File.ReadAllText(_fileName), Contains.Substring("pause, args"));
		}

		[Test]
		public void continue_calls_another_instance_with_arguments ()
		{
			Assert.That(File.ReadAllText(_fileName), Contains.Substring("continue, args"));
		}

		[Test]
		public void shutdown_should_call_another_instance_with_arguments ()
		{
			Assert.That(File.ReadAllText(_fileName), Contains.Substring("stop, args"));
		}


		void Call(string target, string args)
		{
			_process = Process.Start(new ProcessStartInfo{
				Arguments = args,
				FileName = Path.GetFullPath(target)
			});
			_process.WaitForExit();
		}
    }
}
