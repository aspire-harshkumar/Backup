using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -2 )]
	[Parallelizable( ParallelScope.Self )]
	class QuickSearch
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected virtual string classID => "QuickSearch";

		private string username;
		private string password;
		private string vaultName;

		protected TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;


		public QuickSearch()
		{
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Search Vault.mfb", users );

			this.vaultName = this.mfContext.VaultName;

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
		/// Assertion helper for checking that object is visible in listing and producing 
		/// informative assertion message if the object is not visible.
		/// </summary>
		/// <param name="objectName">Object that should be in listing.</param>
		/// <param name="listing">ListView page object.</param>
		private void AssertObjectIsInListing( string objectName, ListView listing )
		{
			Assert.True( listing.IsItemInListing( objectName ),
				$"Expected object '{objectName}' is not visible in listing." );
		}

		/// <summary>
		/// Assertion helper for checking that object is not visible in listing and producing 
		/// informative assertion message if the object is visible.
		/// </summary>
		/// <param name="objectName">Object that should not be in listing.</param>
		/// <param name="listing">ListView page object.</param>
		private void AssertObjectIsNotInListing( string objectName, ListView listing )
		{
			Assert.False( listing.IsItemInListing( objectName ),
				$"Object '{objectName}' is visible in listing when it should not be visible." );
		}

		/// <summary>
		/// Expected object should be listed based on the different search keywords.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"\"NEW YORK — June 6, 2007 — Nullam eu tellus dignissim arcu molestie sodales.\"",
			"Project Announcement / Austin.pdf;Press Release: Opening of Hospital Expansion.doc;Press Release: Own Company, Inc. Established.doc",
			3,
			Description = "Search keyword within quotes [For e.g.: \"Document content\"]." )]
		[TestCase(
			"7720~~7722",
			"Proposal 7722 - S&C Southwest Power",
			2,
			Description = "A numeric range search with '~~' operator." )]
		[TestCase(
			"1/2004",
			"Invitation to Project Meeting 1/2004.doc",
			3,
			Description = "Search with Date." )]
		[TestCase(
			"Press Release: Opening of Hospital Expansion.doc",
			"Press Release: Opening of Hospital Expansion.doc",
			1,
			Description = "Search with document object name." )]
		[TestCase(
			"Helen Chase",
			"Helen Chase",
			1,
			Description = "Search with non-document object name." )]
		[TestCase(
			"Land Meeting Notice",
			"Invitation to Project Meeting 1/2006",
			1,
			Description = "Search with Multiple/Compound words of MFD" )]
		[TestCase(
			"Expansion Report",
			"Progress report - Hospital Expansion.doc",
			1,
			Description = "Search with Multiple words of Document." )]
		public virtual void DifferentKeywordSearch(
			string searchKeyword,
			string expectedObjectsString,
			int expectedObjectsCount )
		{
			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Perform the quick search with mentioned search keyword.
			ListView listing = homePage.SearchPane.QuickSearch( searchKeyword );

			// Store the expected objects in the variable.
			List<string> expectedObjects = StringSplitHelper.ParseStringToStringList( expectedObjectsString, ';' );

			// Assert that the number of search results is as expected.
			Assert.AreEqual( expectedObjectsCount, listing.NumberOfItems );

			// Assert that each expected object is displayed in listing.
			foreach( string expectedObject in expectedObjects )
			{
				this.AssertObjectIsInListing( expectedObject, listing );
			}
		}


		/// <summary>
		/// Searching for document that has never been checked in.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"New document object",
			"Document",
			"Microsoft Word Document (.docx)",
			".docx" )]
		public virtual void SearchNewDocObjWhichYetToBeCheckedIn(
			string objectName,
			string className,
			string template,
			string extension )
		{
			// Starts the test at HomePage as default user.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Start creating new document which opens template selector.
			TemplateSelectorDialog templateSelector = homePage.TopPane.CreateNewObjectFromTemplate( "Document" );

			// Filter to see blank templates and select the template.
			templateSelector.SetTemplateFilter( TemplateSelectorDialog.TemplateFilter.Blank );
			templateSelector.SelectTemplate( template );

			// Click next button to proceed to the metadata card of the new object.
			MetadataCardPopout popOutMDCard = templateSelector.NextButtonClick();

			// Set class and name.
			popOutMDCard.Properties.SetPropertyValue( "Name or title", objectName );
			popOutMDCard.Properties.SetPropertyValue( "Class", className );

			// Save.
			MetadataCardRightPane mdCard = popOutMDCard.SaveAndDiscardOperations.Save( false );

			// Perform the quick search with mentioned search keyword.
			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			// Assert that newly created object which is yet to be checked-in is listed in the list view.
			Assert.True( listing.IsItemInListing( objectName + extension ), "When searching the newly created document object '" + objectName + extension + "' which is yet to be checked-in is not listed." );

		}
	}
}
