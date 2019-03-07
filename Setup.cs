using System;
using System.IO;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;

namespace Motive.MFiles.vNextUI.Tests
{
	[SetUpFixture]
	class Setup
	{

		/// <summary>
		/// Simple wrapper for tearing down the test vault. However, this
		/// method doesn't throw any exception if destroying the vault for some
		/// reason fails. It only writes the exception to the console. Exception is not
		/// thrown because an issue in tear down is not considered as a fatal error that
		/// should prevent the testing of the web application.
		/// </summary>
		/// <param name="mfContext">Context of the test environment.</param>
		private void TearDownLoginSmokeTestEnvironment( MFilesContext mfContext )
		{
			try
			{
				EnvironmentSetupHelper.TearDownEnvironment( mfContext );
			}
			catch( Exception ex )
			{
				Console.WriteLine( "Exception during tear down of the login smoke test environment/vault." + ex.StackTrace );
			}
		}

		/// <summary>
		/// One time setup method that is run only once. It is run before any of the test classes are run.
		/// This method checks that user can log in to the web application. If the login still fails after
		/// a few retries then none of the tests should be run because they will most probably fail.
		/// </summary>
		[OneTimeSetUp]
		public void LoginSmokeTest()
		{
			string classID = "LoginSmoke";

			// Initialize configurations for the login smoke test based on test context parameters.
			TestClassConfiguration configuration = new TestClassConfiguration( classID, TestContext.Parameters );

			// Only run login smoke test if it is enabled in configuration.
			if( configuration.LoginSmokeEnabled )
			{

				// Define users required by this test class.
				UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

				// Create test vault.
				MFilesContext mfContext =
					EnvironmentSetupHelper.SetupEnvironment( 
						EnvironmentHelper.VaultBackend.Firebird, 
						"Data Types And Test Objects.mfb", 
						users );

				// Create browser manager.
				TestClassBrowserManager browserManager =
					new TestClassBrowserManager(
						configuration,
						mfContext.UsernameOfUser( "user" ),
						mfContext.PasswordOfUser( "user" ),
						mfContext.VaultName );

				// Initialize variables for login smoke test.
				int maxLoginAttempts = 3;
				int currentLoginAttempt = 1;
				bool loginSuccessful = false;
	
				// Keep retrying while login has not yet succeeded and there still are attempts left.
				while( !loginSuccessful && currentLoginAttempt <= maxLoginAttempts )
				{
					try
					{
						// Login to home page.
						HomePage homePage = browserManager.StartTestAtHomePage();

						// Login was successful.
						loginSuccessful = true;
					}
					catch( Exception ex )
					{
						// Exception during login.

						// Check has the max login attempts been reached.
						if( currentLoginAttempt >= maxLoginAttempts )
						{
							// Max attempts has been reached.

							// Capture screenshot.
							string screenshotLocation =
								browserManager.CaptureScreenShot( "LOGIN_SMOKE_FAILURE" );

							// Close the browser if it is still on.
							browserManager.EnsureQuitBrowser();

							// Destroy test vault.
							this.TearDownLoginSmokeTestEnvironment( mfContext );

							// Throw an exception. No tests should be executed because login to the application doesn't seem to work.
							throw new Exception(
								$"Login smoke test failed. Retried login to application home page already {maxLoginAttempts} " + 
								"times and it failed. Rejecting any further testing.", ex );
						}
						else
						{
							// There are still attempts left.

							// Close the browser after a failed attempt.
							browserManager.EnsureQuitBrowser();

							// A login attempt was spent.
							++currentLoginAttempt;
						}
					} // end catch
				} // end while

				// Close the browser after successful login.
				browserManager.EnsureQuitBrowser();

				// Destroy test vault.
				this.TearDownLoginSmokeTestEnvironment( mfContext );
			} // end if
		}

		/// <summary>
		/// GlobalTeardown: This function is used to perform the one time tear down after suite completed.
		/// I) It zips the screenshot folder after all the test execution got completed.
		/// </summary>
		[OneTimeTearDown]
		public void GlobalTeardown()
		{
			// Zip the results folder.
			string resultLocation = TestClassBrowserManager.CurrentContextResultLocation;
			string resultZipLocation = TestClassBrowserManager.ProjectDirectory + "\\Reports";			
			ZipHelper.ZipFolder( resultLocation, resultZipLocation, "AutomationTestResult" );
		}
	}
}