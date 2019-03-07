using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.Listing;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -9 )]
	[Parallelizable( ParallelScope.Self )]
	class PreviewFileContents
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected virtual string classID => "PreviewFileContents";

		protected string username;
		protected string password;
		protected string vaultName;

		protected TestClassConfiguration configuration;

		protected MFilesContext mfContext;

		protected TestClassBrowserManager browserManager;

		// Additional Assert messages.
		private readonly string AssertMessageForPageCountMismatch = "Mismatch between the expected and actual page count in preview tab.";
		private readonly string AssertMessageForPreviewStatusMismatch = "Mismatch between the expected and actual preview status in preview tab.";

		public PreviewFileContents()
		{
		}

		[OneTimeSetUp]
		public virtual void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Preview And Annotations.mfb", users );

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

			EnvironmentSetupHelper.TearDownEnvironment( this.mfContext );
		}


		[TearDown]
		public void EndTest()
		{

			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}


		/// <summary>
		/// Select an object and switch from metadata tab to preview tab. Verify that expected number
		/// of content pages are displayed in preview.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Project Schedule (Sales Strategy Development).pdf", 2, Description = "pdf" )]
		[TestCase( "Preview document.docx", 4, Description = "docx" )]
		[TestCase( "Annotated powerpoint.ppt", 5, Description = "Annotated ppt document" )]
		public virtual void PreviewDocument( string objectName, int expectedPageCount )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			listing.SelectObject( objectName );

			// Switch from metadata to preview tab.
			PreviewPane preview = homePage.TopPane.TabButtons.PreviewTabClick();

			// Assert that expected number of content pages are displayed in preview.
			Assert.AreEqual( expectedPageCount, preview.FileContentPageCount, AssertMessageForPageCountMismatch );
		}

		/// <summary>
		/// Expand relationships of an object with attached files. Select the attached file
		/// to automatically display its preview. Verify that expected number of content pages 
		/// are displayed in preview.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Preview mfd", "Preview attached file.docx", 5, Description = "MFD document with attached file." )]
		[TestCase( "Preview assignment", "Preview attached presentation.pptx", 9, Description = "Assignment object with attached file." )]
		public virtual void PreviewAttachedFile( string objectName, string attachedFileName, int expectedPageCount )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( objectName );

			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName );
			relationships.ExpandRelationships();

			// Select attached file which automatically switches to preview tab.
			PreviewPane preview = relationships.SelectAttachedFileForPreview( attachedFileName );

			// Assert that expected number of content pages are displayed in preview.
			Assert.AreEqual( expectedPageCount, preview.FileContentPageCount, AssertMessageForPageCountMismatch );

			// Select the main object after previewing attached file to verify that tab changes back automatically.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
		}

		/// <summary>
		/// Switch to preview tab and then select different documents to view their previews.
		/// Verify that expected number of content pages are displayed in preview.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Preview presentation.pptx;Preview bitmap.bmp;Preview text.txt", "8;1;14", "preview" )]
		public virtual void PreviewSeveralDocumentsInRow( string objects, string pageCounts, string searchword )
		{
			List<string> objectsToPreview = StringSplitHelper.ParseStringToStringList( objects, ';' );
			List<string> pageCountStrings = StringSplitHelper.ParseStringToStringList( pageCounts, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( searchword );

			// Switch to preview tab before selecting any object.
			homePage.TopPane.TabButtons.PreviewTabClick();

			// Go through objects.
			for( int i = 0; i < objectsToPreview.Count; ++i )
			{
				// Select object when preview tab is already open.
				PreviewPane preview = listing.SelectObjectForPreview( objectsToPreview.ElementAt( i ) );

				int expectedPageCount = Int32.Parse( pageCountStrings.ElementAt( i ) );

				// Assert that expected number of content pages are displayed in preview.
				Assert.AreEqual( expectedPageCount, preview.FileContentPageCount, AssertMessageForPageCountMismatch );
			}
		}

		/// <summary>
		/// Select object, switch from metadata tab to preview tab, and then back to metadata tab.
		/// Then select another object and repeat. Verify that expected number of content pages are 
		/// displayed in preview.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Preview document.docx;Preview bitmap.bmp;Preview worksheet.xlsx",
			"4;1;5",
			"1. Documents>By Customer>Davis & Cobb, Attorneys at Law" )]
		public virtual void PreviewAndMetadataTabsSwitchSeveralDocumentsInRow(
			string objects,
			string pageCounts,
			string view )
		{
			List<string> objectsToPreview = StringSplitHelper.ParseStringToStringList( objects, ';' );
			List<string> pageCountStrings = StringSplitHelper.ParseStringToStringList( pageCounts, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.ListView.NavigateToView( view );

			// Go through objects.
			for( int i = 0; i < objectsToPreview.Count; ++i )
			{
				string currentObject = objectsToPreview.ElementAt( i );

				// Select object to view metadata.
				listing.SelectObject( currentObject );

				int expectedPageCount = Int32.Parse( pageCountStrings.ElementAt( i ) );

				// Switch to preview tab.
				PreviewPane preview = homePage.TopPane.TabButtons.PreviewTabClick();

				// Assert that expected number of content pages are displayed in preview.
				Assert.AreEqual( expectedPageCount, preview.FileContentPageCount, AssertMessageForPageCountMismatch );

				// Switch back to metadata tab.
				homePage.TopPane.TabButtons.MetadataTabClick( currentObject );
			}
		}

		/// <summary>
		/// Select non-document object and switch from metadata tab to preview tab.
		/// No preview should be shown for non-document object.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "City of Chicago (Planning and Development)", "Customer" )]
		public virtual void NoPreviewForNonDocumentObject( string objectName, string objectType )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			listing.SelectObject( objectName );

			// Switch from metadata to preview tab.
			PreviewPane preview = homePage.TopPane.TabButtons.PreviewTabClick();

			// Assert that there should be nothing to preview.
			Assert.AreEqual( PreviewPane.PreviewStatus.NothingToPreview, preview.Status, AssertMessageForPreviewStatusMismatch );
		}

		/// <summary>
		/// Select document object for which the preview is not supported.
		/// There should be error in preview.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Window Chart E14.dwg", "Document" )]
		public virtual void NoPreviewForUnsupportedFileType( string objectName, string objectType )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			listing.SelectObject( objectName );

			// Switch from metadata to preview tab.
			PreviewPane preview = homePage.TopPane.TabButtons.PreviewTabClick();

			// Assert that preview should be unavailable.
			Assert.AreEqual( PreviewPane.PreviewStatus.PreviewUnavailable, preview.Status, AssertMessageForPreviewStatusMismatch );
		}

		/// <summary>
		/// Switch to preview tab and then alternate between selecting document and non-document/mfd object.
		/// Document objects should automatically display preview and non-document/mfd objects should automatically
		/// display metadata tab.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Order - Graphical Design.doc",
			"Daniel Hall",
			"Sales Invoice 401 - ESTT Corporation (IT).xls",
			"Order - Logo Design",
			"estt corporation",
			"Contact person",
			"Contact persons" )]
		public virtual void PreviewAndMetadataTabsAutomaticSwitchSeveralObjectsInRow(
			string doc1,
			string obj1,
			string doc2,
			string obj2,
			string searchWord,
			string objectType,
			string objectTypeHeader )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Make a search with filters: Document and another non-document object type.
			SearchFilters filters = homePage.SearchPane.TypeSearchWord( searchWord );
			filters.ShowMoreObjectTypeFilters();
			filters.SetFilter( "Document" ).SetFilter( objectType );

			ListView listing = homePage.SearchPane.ClickSearchButton();

			// Expand the grouping header of the other object type.
			listing.GroupingHeaders.ExpandGroup( objectTypeHeader );

			// Switch to preview tab before selecting any object.
			homePage.TopPane.TabButtons.PreviewTabClick();

			// Select first document which should display the preview automatically.
			PreviewPane previewDoc1 = listing.SelectObjectForPreview( doc1 );

			Assert.AreEqual( PreviewPane.PreviewStatus.ContentDisplayed, previewDoc1.Status, AssertMessageForPreviewStatusMismatch );

			// Select non-document which should display the metadata card automatically.
			MetadataCardRightPane mdCardObj1 = listing.SelectObject( obj1 );

			// Select another document which should display the preview automatically.
			PreviewPane previewDoc2 = listing.SelectObjectForPreview( doc2 );

			Assert.AreEqual( PreviewPane.PreviewStatus.ContentDisplayed, previewDoc2.Status, AssertMessageForPreviewStatusMismatch );

			// Select non-document/mfd object which should display the metadata card automatically.
			MetadataCardRightPane mdCardObj2 = listing.SelectObject( obj2 );
		}

		/// <summary>
		/// Verify that an annotation object of annotated document is found and preview is opened for
		/// an annotation object.
		/// </summary>
		[Test]
		[TestCase( "Annotated word.doc", "Annotations for Annotated document.doc v1 (TinaS)", 1 )]
		[TestCase( "Annotated excel.xlsx", "Annotations for Annotated excel.xlsx v1 (TinaS)", 1 )]
		[TestCase( "Annotated powerpoint.ppt", "Annotations for Annotated powerpoint.ppt v2 (TinaS)", 5 )]
		public virtual void PreviewDocumentBySelectingAnnotationObject(
			string documentName,
			string attachedFileName,
			int expectedPageCount )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// First search for annotated document.
			ListView listing = homePage.SearchPane.QuickSearch( documentName );

			// Expand relationships of annotated document.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( documentName );
			relationships.ExpandRelationships();
			relationships.ExpandRelationshipHeader( "Annotations" );

			// Select attached file which automatically switches to preview tab.
			PreviewPane preview = relationships.SelectRelatedObjectForPreview( "Annotations", attachedFileName );

			// Assert if preview is not opened for annotation object.
			Assert.AreEqual( PreviewPane.PreviewStatus.ContentDisplayed, preview.Status, AssertMessageForPreviewStatusMismatch );

			// Assert if expected number of content pages are not displayed in preview.
			Assert.AreEqual( expectedPageCount, preview.FileContentPageCount, AssertMessageForPageCountMismatch );

		}
	}
}