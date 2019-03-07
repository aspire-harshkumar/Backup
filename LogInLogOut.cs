using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MFilesAPI;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OpenQA.Selenium;
using static Motive.MFiles.vNextUI.Utilities.WebDriverFactory;

namespace Motive.MFiles.vNextUI.Tests
{

	[Order( -3 )]
	[Parallelizable( ParallelScope.Self )]
	public class LogInLogOut
	{

		private readonly static string ErrorMessageMismatch = "Mismatch between expected and actual error message.";
		private readonly static string LoginPageNotLoadedMessage = "User is not at login page after login failure.";

		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		private string username;
		private string password;
		private string vaultName;

		private string controlObjectName = "login control object";

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		public LogInLogOut()
		{
			this.classID = "LogInLogOut";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetDifferentTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Sample Vault.mfb", users );

			// Disable the login account of this user.
			EnvironmentSetupHelper.DisableLoginAccount( this.mfContext, "logindisabled" );

			// Disable the user account of this user.
			EnvironmentSetupHelper.DisableUserAccount( this.mfContext, "userdisabled" );

			// Create one test document object that all users can access.
			EnvironmentSetupHelper.CreateTestObject( mfContext, this.controlObjectName, "admin" );
			this.controlObjectName += ".pdf";

			this.vaultName = this.mfContext.VaultName;

			// TODO: The "user" identifier here is now defined in SetupHelper. Maybe this should come from configuration and
			// it should also be given to the SetupHelper as parameter.
			this.username = this.mfContext.UsernameOfUser( "user" );
			this.password = this.mfContext.PasswordOfUser( "user" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );

		}

		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( mfContext );
		}


		[TearDown]
		public void EndTest()
		{
			this.browserManager.FinalizeBrowserStateBasedOnTestResultAndLoginPageCheck( TestExecutionContext.CurrentContext );
		}


		/// <summary>
		/// Tests that a user with valid credentials is able to log in to the application.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "admin" )]
		[TestCase( "user" )]
		[TestCase( "vaultadmin" )]
		[TestCase( "readonly" )]
		[TestCase( "external" )]
		public void LogInValidUser( string usernameTestdata )
		{
			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			HomePage homePage = loginPage.Login( this.mfContext.UsernameOfUser( usernameTestdata ), this.mfContext.PasswordOfUser( usernameTestdata ), this.vaultName );

			TopPane topPane = homePage.TopPane;

			// TODO: Assert from UI that there is some indication which user is logged in.

			// Assert that vault name is displayed.
			Assert.AreEqual( this.vaultName, topPane.VaultName );

			// Search for the control object by its name.
			ListView listing = homePage.SearchPane.QuickSearch( this.controlObjectName );

			// Verify also that logged in user can make a search.
			Assert.True( listing.IsItemInListing( this.controlObjectName ) );

			loginPage = topPane.Logout();

		}

		/// <summary>
		/// Tests that a user with valid credentials is able to log out from the application and returns to the login page.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "admin" )]
		[TestCase( "user" )]
		[TestCase( "vaultadmin" )]
		[TestCase( "readonly" )]
		[TestCase( "external" )]
		public void LogOutValidUser( string usernameTestdata )
		{
			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			TopPane topPane = loginPage.Login( this.mfContext.UsernameOfUser( usernameTestdata ), this.mfContext.PasswordOfUser( usernameTestdata ), this.vaultName ).TopPane;

			loginPage = topPane.Logout();

			Assert.True( loginPage.IsLoaded, LoginPageNotLoadedMessage );
			// TODO: Should this also assert that the session does not exist in server? By some API call?
		}


		/// <summary>
		/// Tests that user cannot log in and error is displayed when using wrong password containing an extra character.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "user" )]
		[TestCase( "admin" )]
		[TestCase( "vaultadmin" )]
		public void WrongPasswordAuthenticationFails( string usernameTestdata )
		{

			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			// Enter correct username but password is invalid because it contains an extra character.
			loginPage.EnterCredentials( mfContext.UsernameOfUser( usernameTestdata ), mfContext.PasswordOfUser( usernameTestdata ) + "a" );

			Assert.AreEqual( "Authentication failed.", loginPage.LoginErrorText,
				ErrorMessageMismatch );
			Assert.True( loginPage.IsLoaded, LoginPageNotLoadedMessage );
		}

		/// <summary>
		/// Tests that user cannot log in and error is displayed when using empty password.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "user" )]
		[TestCase( "admin" )]
		[TestCase( "vaultadmin" )]
		public void EmptyPasswordAuthenticationFails( string usernameTestdata )
		{

			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			// Enter correct username but password is invalid because it is empty.
			loginPage.EnterCredentials( mfContext.UsernameOfUser( usernameTestdata ), "" );

			Assert.AreEqual( "Authentication failed.", loginPage.LoginErrorText,
				ErrorMessageMismatch );
			Assert.True( loginPage.IsLoaded, LoginPageNotLoadedMessage );
		}

		/// <summary>
		/// Tests that user cannot log in to and error is displayed when using wrong/non-existing/empty username.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "not_user", "password", "Authentication failed." )]
		[TestCase( "", "", "Username cannot be empty." )]
		[TestCase( "", "password", "Username cannot be empty." )]
		public void WrongUsernameAuthenticationFails(
			string usernameTestdata,
			string passwordTestdata,
			string expectedErrorMessage )
		{

			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			// Enter non-existent username and password.
			loginPage.EnterCredentials( usernameTestdata, passwordTestdata );

			Assert.AreEqual( expectedErrorMessage, loginPage.LoginErrorText,
				ErrorMessageMismatch );
			Assert.True( loginPage.IsLoaded, LoginPageNotLoadedMessage );
		}

		/// <summary>
		/// Tests that user cannot log in and error is displayed with missing license, or disabled login account or user account.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"nolicense",
			"You cannot log in because your account has not been assigned a license." )]
		[TestCase(
			"logindisabled",
			"Access denied. Your login account is currently disabled. (Account name: \"{0}\")" )]
		[TestCase(
			"userdisabled",
			"Access denied. Your user account in this document vault is currently disabled. (Account name: \"{0}\")" )]
		public void AccountAndLicenseErrorsAuthenticationFails(
			string usernameTestdata,
			string errorMessageTestdata )
		{

			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			string username = mfContext.UsernameOfUser( usernameTestdata );

			loginPage.EnterCredentials( username, mfContext.PasswordOfUser( usernameTestdata ) );

			// Format the expected error message. The username is inserted to the placeholder in
			// the string if there is a placeholder.
			string expectedErrorMessage = string.Format( errorMessageTestdata, username );

			Assert.AreEqual( expectedErrorMessage, loginPage.LoginErrorText,
				ErrorMessageMismatch );
			Assert.True( loginPage.IsLoaded, LoginPageNotLoadedMessage );
		}

		/// <summary>
		/// Tests that a user able to log in to the application and Logout from Home view ( default view ).
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase( "user" )]
		public void LogOutFromHomeView( string usernameTestdata )
		{
			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			HomePage homePage = loginPage.Login( this.mfContext.UsernameOfUser( usernameTestdata ), this.mfContext.PasswordOfUser( usernameTestdata ), this.vaultName );

			TopPane topPane = homePage.TopPane;

			loginPage = topPane.Logout();
		}

		/// <summary>
		/// Tests that a user able to log in to the application and Logout from Recently accessed by me.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase( "user" )]
		public void LogOutFromRecentView( string usernameTestdata )
		{
			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			HomePage homePage = loginPage.Login( this.mfContext.UsernameOfUser( usernameTestdata ), this.mfContext.PasswordOfUser( usernameTestdata ), this.vaultName );

			TopPane topPane = homePage.TopPane;

			// Navigate to Recently accessed by me view.
			topPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Recent );

			loginPage = topPane.Logout();
		}

		/// <summary>
		/// Tests that a user able to log in to the application and Logout from Favorites view.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase( "user" )]
		public void LogOutFromFavoritesView( string usernameTestdata )
		{
			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			HomePage homePage = loginPage.Login( this.mfContext.UsernameOfUser( usernameTestdata ), this.mfContext.PasswordOfUser( usernameTestdata ), this.vaultName );

			TopPane topPane = homePage.TopPane;

			// Navigate to Favorites view.
			topPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Favorites );

			loginPage = topPane.Logout();
		}

		/// <summary>
		/// Tests that a user able to log in to the application and Logout from Assigned to me view.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase( "user" )]
		public void LogOutFromAssignedToMeView( string usernameTestdata )
		{
			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			HomePage homePage = loginPage.Login( this.mfContext.UsernameOfUser( usernameTestdata ), this.mfContext.PasswordOfUser( usernameTestdata ), this.vaultName );

			TopPane topPane = homePage.TopPane;

			// Navigate to Assigned to me view.
			topPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Assigned );

			loginPage = topPane.Logout();
		}

		/// <summary>
		/// Tests that a user able to log in to the application and Logout from Checkout to me view.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase( "user" )]
		public void LogOutFromCheckoutToMeView( string usernameTestdata )
		{
			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			HomePage homePage = loginPage.Login( this.mfContext.UsernameOfUser( usernameTestdata ), this.mfContext.PasswordOfUser( usernameTestdata ), this.vaultName );

			TopPane topPane = homePage.TopPane;

			// Navigate to Checkout to me view.
			homePage.ListView.GroupingHeaders.ExpandGroup( "Other Views" );
			ListView listing = homePage.ListView.NavigateToView( "Checked Out to Me" );

			loginPage = topPane.Logout();
		}

		/// <summary>
		/// Tests that a user able to log in to the application and Logout from Search view.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase( "user",
			"Document",
			"project property.docx" )]
		public void LogOutFromSearchView( string usernameTestdata,
			String objectType,
			String objectName )
		{
			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			HomePage homePage = loginPage.Login( this.mfContext.UsernameOfUser( usernameTestdata ), this.mfContext.PasswordOfUser( usernameTestdata ), this.vaultName );

			TopPane topPane = homePage.TopPane;

			// Navigate to Search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			loginPage = topPane.Logout();
		}

		/// <summary>
		/// Tests that a user able to log in to the application and Logout from Common views.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase( "user",
			"1. Documents>Pictures" )]
		public void LogOutFromCommonViews( string usernameTestdata,
			String view )
		{
			LoginPage loginPage = this.browserManager.StartTestAtLoginPage();

			HomePage homePage = loginPage.Login( this.mfContext.UsernameOfUser( usernameTestdata ), this.mfContext.PasswordOfUser( usernameTestdata ), this.vaultName );

			TopPane topPane = homePage.TopPane;

			// Navigate to Common views.
			ListView listing = homePage.ListView.NavigateToView( view );

			loginPage = topPane.Logout();
		}
	}
}
