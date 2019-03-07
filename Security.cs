using System;
using System.Threading;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.WebDriverHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;
using OpenQA.Selenium;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -10 )]
	[Parallelizable( ParallelScope.Self )]
	class Security
	{

		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		private string username;
		private string password;
		private string vaultName;

		/// <summary>
		/// Additional assert messages to be printed with the assert methods.
		/// </summary>
		private static readonly string wildCardErrorMessage =
			"The search request could not be completed. Wildcard characters cannot be used as first characters in a search request.";

		private static readonly string dialogerrorMsg = "Not found";

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		public Security(  )
		{
			this.classID = "Security";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the back end.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Security Vault.mfb", users );

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
			// Returns the browser to the home page to be used by the next test or quits the browser if
			// the test failed.
			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		/// <summary>
		/// Testing that the  Property (integer) overflows by supplying greater than a data type's maximum value to existing object.
		/// Overflow error message " please enter integer " should be shown.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase(
			"Document",
			"Integer overflow.docx",
			"Integer Value",
			"123456",
			"1234567890123456789",
			"Please enter an integer." )]
		public void PropertyOverflowErrorMessageDisplayedToExistingObject(
			string objectType,
			string objectName,
			string propertyName,
			string validIntValue,
			string invalidIntValue,
			string errorMsg )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadata card of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set integer value in MetaData Card.
			metadataCardRightPane.Properties.SetPropertyValue( propertyName, validIntValue );

			// Save the operations.
			metadataCardRightPane.SaveAndDiscardOperations.Save();

			// Verify the integer value is set as expected in the right pane metadata card.
			Assert.AreEqual( validIntValue, metadataCardRightPane.Properties.GetPropertyValue( propertyName ),
				"Expected Property value did not match the actual value" );

			// Set invalid integer value in MetaData Card.
			metadataCardRightPane.Properties.SetPropertyValue( propertyName, invalidIntValue );

			// Verify the error message.
			Assert.AreEqual( errorMsg, metadataCardRightPane.Properties.GetPropertyMessage( propertyName ), "Property overflow error message did not match the actual error." );

			// Discard changes.
			metadataCardRightPane.DiscardChanges();

		}

		/// <summary>
		/// Testing that the object name should not be in BOLD when HTML tags are used to existing object.
		/// object name should show the HTML tags.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase(
			"Document",
			"Sample one.xlsx",
			"<b>Test Sample</b>" )]
		public void HTMLTagsAreNotExecutedInObjectName(
			string objectType,
			string objectName,
			string tagHtml )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadata card of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set name value in MetaData Card.
			metadataCardRightPane.Properties.SetPropertyValue( "Name or title", tagHtml );

			// Save the operations.
			metadataCardRightPane.SaveAndDiscardOperations.Save();

			// Verify the name value is set as expected in the right pane metadata card.
			Assert.AreEqual( tagHtml, metadataCardRightPane.Properties.GetPropertyValue( "Name or title" ),
				"Expected name did not match the actual value." );

		}

		/// <summary>
		/// Testing that the input and output is handled with special characters.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase(
			"Document",
			"wildcard.txt",
			"#@#$$***%^&%!",
			"@#@!)()**#11_!" )]
		public void InputAndOutputIsHandledWithSpecialCharacters(
			string objectType,
			string objectName,
			string searchSpecialChar,
			string newWildCardName )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view and enter wild card characters then search.
			MessageBoxDialog errorDialog = homePage.SearchPane.FilteredQuickSearchAndWaitForMessageBox( searchSpecialChar, objectType );

			// Verify if the dialog contains expected error text.
			Assert.AreEqual( wildCardErrorMessage, errorDialog.Message, "Mismatch between expected and actual error dialog message." );

			// Accept the dialog.
			errorDialog.OKClick();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadata card of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Set name value in MetaData Card.
			metadataCardRightPane.Properties.SetPropertyValue( "Name or title", newWildCardName );

			// Save the operations.
			metadataCardRightPane.SaveAndDiscardOperations.Save();

			// Verify the integer value is set as expected in the right pane metadata card.
			Assert.AreEqual( newWildCardName, metadataCardRightPane.Properties.GetPropertyValue( "Name or title" ), "Expected name value did not match the actual value" );
		}

		/// <summary>
		/// Testing that the Java scripts is not executed.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase(
			"Document",
			"script<script>alert( \"Hello, I am a javascript bug!\" )</script>",
			"<script>alert( \"Hello, I am a javascript bug!\" )</script>",
			"Keywords",
			Description = "Encoding is not applied to the script tags used in Input fields , Search fields and URL for document Object" )]
		[TestCase(
			"Assignment",
			"Assignment<script>alert( \"Hello, I am a javascript bug!\" )</script>",
			"<script>alert( \"Hello, I am a javascript bug!\" )</script>",
			"Assignment description",
			Description = "Encoding is not applied to the script tags used in Input fields , Search fields and URL for Non document Object" )]
		public void JavaScriptIsNotExecuted(
			string objectType,
			string objectName,
			string javascript,
			string propertyName )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigates to the search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Open metadata card of the test object.
			MetadataCardRightPane metadataCardRightPane = listing.SelectObject( objectName );

			// Verify the Name is displayed as expected in the right pane metadata card.
			Assert.AreEqual( objectName, metadataCardRightPane.Properties.GetPropertyValue( "Name or title" ),
				"Expected name value did not match the actual value" );

			// Set property value in Metadata Card.
			metadataCardRightPane.Properties.SetPropertyValue( propertyName, javascript );

			// Verify if the expected property value displayed.
			Assert.AreEqual( javascript, metadataCardRightPane.Properties.GetPropertyValue( propertyName ),
				"Mismatch between expected and actual property message." );

			// Save the operations.
			metadataCardRightPane.SaveAndDiscardOperations.Save();

		}

		/// <summary>
		/// Testing that the closing the browser 
		/// and relaunch the URL, user should navigate to login page.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase(
			"1. Documents" )]
		public void CopiedURLSessionGetsExpired(
			string view )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			TopPane topPane = homePage.TopPane;

			// Navigate to Common views.
			ListView listing = homePage.ListView.NavigateToView( view );

			// Get the current URL.
			string firstcurrenturl = this.browserManager.CurrentUrl;

			// Close the browser.
			this.browserManager.EnsureQuitBrowser();

			// Restart browser and Navigate to the URL then wait until login page is loaded.
			LoginPage loginpage = this.browserManager.RestartBrowserAndNavigateToMFilesUrl( firstcurrenturl );

			// Verify if login page is displayed.
			Assert.True( loginpage.IsLoaded, "Login Page is not displayed" );

			// Get the current URL.
			String currenturl = this.browserManager.CurrentUrl;

			// Modify the String to match URL after first login, to justify if browser navigated to correct URL and
			// justify if view ID and Vault ID is correct.
			String modifiedURL = currenturl.Replace( "%2F", "/" ).Replace( "%7B", "{" ).Replace( "%7D", "}" );
			modifiedURL = modifiedURL.Replace( "login?ReturnUrl=/", "" );

			// Verify if browser navigated to correct URL.
			Assert.AreEqual( firstcurrenturl, modifiedURL, "Browser did not navigate to correct URL" );

			// Close the browser.
			this.browserManager.EnsureQuitBrowser();

		}

		/// <summary>
		/// Testing the session cookie duration (expires and max-age).
		/// </summary>
		[Test]
		[Category( "Security" )]
		public void CookieSessionExpiry()
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Get local date time now.
			DateTime now = DateTime.Now;

			// Declare a string variable to store expiry date in cookie.
			string expiry = null;

			// Get all cookies.
			foreach( Cookie ck in this.browserManager.CurrentCookies )
			{
				// Date format to remove the seconds from the date.
				string format = "MM/dd/yyyy hh:mm tt";

				// Converting cookie expiry date to required format string.
				expiry = ck.Expiry.Value.ToString( format );

				// Verify if the expiry date, this should set to tomorrow same time.
				Assert.AreEqual( now.AddDays( 1.00 ).ToString( format ), expiry, "Expiry Date is not as per expected value" );
			}
		}

		/// <summary>
		/// Testing the session tokens for cookie flags (httpOnly and secure).
		/// </summary>
		[Test]
		[Category( "Security" )]
		public void SessionTokensContainsCookieFlags()
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Declare a boolean variables to store HTTP only flag and secure flag in cookie.
			Boolean httponlyflag = false;
			Boolean secureflag = false;

			// Get all cookies.
			foreach( Cookie ck in this.browserManager.CurrentCookies )
			{
				httponlyflag = ck.IsHttpOnly;
				secureflag = ck.Secure;

				// Verify if the cookie contains httpOnly and secure flags.
				Assert.True( httponlyflag, "The cookie does not contain httpOnly flag." );
				Assert.False( secureflag, "The cookie contains Secure flag." );
			}
		}

		/// <summary>
		/// Testing the Authentication details  not  stored in cookies in the clear format.
		/// </summary>
		[Test]
		[Category( "Security" )]
		public void AuthDetailsNotPresentInCookie()
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Declare a string variable to store cookie name and value in cookie.
			string cookiename = null, cookievalue = null;

			// Get all cookies.
			foreach( Cookie ck in this.browserManager.CurrentCookies )
			{
				cookiename = ck.Name.ToString();
				cookievalue = ck.Value.ToString();

				// Verify if the cookie does not contain user name.
				Assert.False( cookiename.Contains( username ),
					"Cookie name contains user name. [Cookie name - '" + cookiename + "']; User name: '" + username + "'" );
				Assert.False( cookievalue.Contains( username ),
					"Cookie value contains user name. [Cookie value - '" + cookievalue + "']; User name: '" + username + "'" );

				// Verify if the cookie does not contain password.
				Assert.False( cookiename.Contains( password ),
					"Cookie name contains password. [Cookie name - '" + cookiename + "']; Password: '" + password + "'" );
				Assert.False( cookievalue.Contains( password ),
					"Cookie value contains password. [Cookie name - '" + cookievalue + "']; Password: '" + password + "'" );
			}
		}

		/// <summary>
		/// Testing that the Current URL does not contains any sensitive data.
		/// </summary>
		[Test]
		[Category( "Security" )]
		public void URLDoesNotContainUsernameOrPassword()
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Get the current URL.
			string currenturl = this.browserManager.CurrentUrl;

			// Verify if the URL does not contain user name.
			Assert.False( currenturl.Contains( username ), "URL contains user name." );

			// Verify if the URL does not contain password.
			Assert.False( currenturl.Contains( password ), "URL contains password." );

		}

		/// <summary>
		/// Testing that the Java scripts is not executed in URL.
		/// </summary>
		[Test]
		[Category( "Security" )]
		[TestCase(
			"1. Documents>Pictures",
			"<script>alert( \"Hello, I am a javascript bug!\" )</script>" )]
		public void JavaScriptIsNotExecutedInURL(
			string view,
			string javascript )
		{
			// Start the test at HomePage.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to common view
			ListView listing = homePage.ListView.NavigateToView( view );

			// Get the current URL.
			string currenturl = this.browserManager.CurrentUrl;

			// Add Java script to current URL
			string urlWithJavascript = currenturl + javascript;

			// Go to given URL then Wait until message dialog box for error is loaded.
			MessageBoxDialog errorDialog = this.browserManager.NavigateToUrlAndWaitForErrorDialog( urlWithJavascript );

			// Verify if the dialog contains text.
			Assert.AreEqual( dialogerrorMsg, errorDialog.Message, "Mismatch between expected and actual dialog message." );

			// Accept the dialog.
			errorDialog.OKClick();

		}
	}
}
