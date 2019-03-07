using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -10 )]
	[Parallelizable( ParallelScope.Self )]
	class DragAndDropCreateDocuments
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		// Note that this folder path containing all the required test files should exist in 
		// all Selenium node machines where the tests may be executed.
		private static readonly string DragAndDropFilesFolderPath = "C:\\vNextDragAndDrop\\DragAndDropCreateDocuments\\";

		private string username;
		private string password;
		private string vaultName;

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		public DragAndDropCreateDocuments()
		{
			this.classID = "DragAndDropCreateDocuments";
		}


		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Data Types And Test Objects.mfb", users );

			this.vaultName = this.mfContext.VaultName;

			// TODO: The "user" identifier here is now defined in SetupHelper. Maybe this should come from configuration and
			// it should also be given to the SetupHelper as parameter.
			this.username = this.mfContext.UsernameOfUser( "user" );
			this.password = this.mfContext.PasswordOfUser( "user" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );

		}

		/// <summary>
		/// After all tests have been run, ensure that the browser is closed. Then destroy the 
		/// test environment of the test class.
		/// </summary>
		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( this.mfContext );
		}


		/// <summary>
		/// After each test, navigate back to home page if test has passed. But if the test has failed,
		/// then the browser is closed to ensure fresh start for next test.
		/// </summary>
		[TearDown]
		public void EndTest()
		{

			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		/// <summary>
		/// Helper method to add folder path to file names. So, this method returns
		/// full file paths to the test files that can be dragged and dropped.
		/// </summary>
		/// <param name="fileNames">File names as a list.</param>
		/// <returns>Full file paths to the test files.</returns>
		private List<string> GetFilePathsOfTestFiles( List<string> fileNames )
		{
			// List to be returned.
			List<string> filePaths = new List<string>();

			// Go through each file.
			for( int i = 0; i < fileNames.Count; ++i )
			{
				// Add file name after the folder path string. Add the resulting full path
				// to the list.
				filePaths.Add( DragAndDropFilesFolderPath + fileNames[ i ] );
			}

			// Return full file paths as a list.
			return filePaths;
		}

		/// <summary>
		/// Helper method to go through newly created documents and verify their metadata 
		/// properties. Can also be used to check workflow and state.
		/// </summary>
		/// <param name="mdCard">Metadata card page object of
		/// the currently selected object.</param>
		/// <param name="homePage">HomePage page object.</param>
		/// <param name="documentNames">Document object names as a list.
		/// Should also contain file extensions.</param>
		/// <param name="documentClass">Expectd class of the documents.</param>
		/// <param name="property">Property to be checked.</param>
		/// <param name="propertyValue">Expected property value.</param>
		/// <param name="workflow">Workflow to be checked.</param>
		/// <param name="workflowState">Workflow state property value.</param>
		private void VerifyProperties(
			MetadataCardRightPane mdCard,
			HomePage homePage,
			string currentFileName,
			string documentClass,
			string property,
			string propertyValue,
			string workflow = null,
			string workflowState = null	 )
		{				
				// Assert that the property values are set to all objects.
				Assert.AreEqual( documentClass, mdCard.Properties.GetPropertyValue( "Class" ),
					$"Class property value mismatch for object '{currentFileName}'" );
				Assert.AreEqual( propertyValue, mdCard.Properties.GetPropertyValue( property ),
					$"{property} property value mismatch for object '{currentFileName}'" );

				// Assert metadata card's workflow if the parameter is not null.
				if( workflow != null )
				{
					Assert.AreEqual( workflow, mdCard.Workflows.Workflow,
						$"Workflow mismatch for object '{currentFileName}'" );
				}

				// Assert metadata card's workflow state if the parameter is not null.
				if( workflowState != null )
				{
					Assert.AreEqual( workflowState, mdCard.Workflows.WorkflowState,
						$"Workflow state mismatch for object '{currentFileName}'" );
				}
		}

		/// <summary>
		/// Helper method to select a document object after drag and dropping multiple files
		/// and creating the with Create all button. The new documents may be created in
		/// somewhat random order so it is possible that the verification is started with
		/// the already selected object. This method doesn't select the first object if it is
		/// already selected.
		/// </summary>
		/// <param name="isFirstItem">True if current file name is first item being checked</param>
		/// <param name="listing"> Listing page object.</param>
		/// <param name="currentFileName">Document object name as a String.
		/// Should also contain file extensions.</param>
		private void SelectObjectIfNecessary( bool isFirstItem,
			string currentFileName,
			ListView listing )
		{
			// Check if this is not the first item to be selected.
			if( !isFirstItem )
			{
				// This is not the first item. Select it because currently the previous item is selected.
				listing.SelectObject( currentFileName );
			}
			else
			{
				// This is the first item. 
				// Check if it was not randomly automatically selected when multiple documents were created by drag and drop.
				if( listing.SelectedItemName != currentFileName )
				{
					// The item was not already selected. Select it.
					listing.SelectObject( currentFileName );
				}
			}
		}

		/// <summary>
		/// Helper method to go through newly created documents and verify their metadata 
		/// and preview file content. Can also be used to check workflow and state.
		/// </summary>
		/// <param name="mdCard">Metadata card page object of
		/// the currently selected object.</param>
		/// <param name="homePage">HomePage page object.</param>
		/// <param name="documentNames">Document object names as a list.
		/// Should also contain file extensions.</param>
		/// <param name="previewPageCounts">Preview page counts of each
		/// document in same order as the document names.</param>
		/// <param name="documentClass">Expectd class of the documents.</param>
		/// <param name="property">Property to be checked.</param>
		/// <param name="propertyValue">Expected property value.</param>
		/// <param name="workflow">Workflow to be checked.</param>
		/// <param name="workflowState">Workflow state property value.</param>
		private void VerifyMultipleCreatedDocuments(
			MetadataCardRightPane mdCard,
			HomePage homePage,
			List<string> documentNames,
			List<string> previewPageCounts,
			string documentClass,
			string property,
			string propertyValue,
			string workflow = null,
			string workflowState = null )
		{
			bool isFirstItem = false;

			// Go through all the created documents.
			for( int j = 0; j < documentNames.Count; ++j )
			{
				string currentFileName = documentNames[ j ];

				isFirstItem = j == 0;

				// Select Object if Necessary.
				this.SelectObjectIfNecessary( isFirstItem, currentFileName, homePage.ListView );

				// Verify properties of object.
				this.VerifyProperties( mdCard, homePage, currentFileName, documentClass, property, propertyValue, workflow, workflowState );

				// Open the preview tab.
				PreviewPane preview = homePage.TopPane.TabButtons.PreviewTabClick();

				// Assert that the file content is displayed in the preview and that
				// the page count is as expected.
				Assert.AreEqual( PreviewPane.PreviewStatus.ContentDisplayed, preview.Status,
					$"Preview status mismatch for object '{currentFileName}'." );
				int expectedPageCount = Int32.Parse( previewPageCounts[ j ] );
				Assert.AreEqual( expectedPageCount, preview.FileContentPageCount,
					$"Page count mismatch for object '{currentFileName}'." );

				// Switch back to the metadata tab.
				homePage.TopPane.TabButtons.MetadataTabClick( documentNames[ j ] );
			}
		}


		/// <summary>
		/// Overloaded Helper method to go through newly created unsupported files and verify their metadata 
		/// and unsupported preview file content. Can also be used to check workflow and state.
		/// </summary>
		/// <param name="mdCard">Metadata card page object of
		/// the currently selected object.</param>
		/// <param name="homePage">HomePage page object.</param>
		/// <param name="documentNames">Document object names as a list.
		/// Should also contain file extensions.</param>
		/// <param name="documentClass">Expected class of the documents.</param>
		/// <param name="property">Property to be checked.</param>
		/// <param name="propertyValue">Expected property value.</param>
		/// <param name="workflow">Workflow to be checked.</param>
		/// <param name="workflowState">Workflow state property value.</param>
		private void VerifyMultipleCreatedDocuments(
			MetadataCardRightPane mdCard,
			HomePage homePage,
			List<string> documentNames,
			string documentClass,
			string property,
			string propertyValue,
			string workflow = null,
			string workflowState = null )
		{
			bool isFirstItem = false;

			// Go through all the created documents.
			for( int j = 0; j < documentNames.Count; ++j )
			{
				string currentFileName = documentNames[ j ];

				isFirstItem = j == 0;

				// Select Object if Necessary.
				this.SelectObjectIfNecessary( isFirstItem, currentFileName, homePage.ListView );

				// Verify properties of object.
				this.VerifyProperties( mdCard, homePage, currentFileName, documentClass, property, propertyValue, workflow, workflowState );

				// Open the preview tab.
				PreviewPane preview = homePage.TopPane.TabButtons.PreviewTabClick();

				// Assert that the file content is unavailable in the preview.
				Assert.AreEqual( PreviewPane.PreviewStatus.PreviewUnavailable, preview.Status,
					$"Preview status mismatch for object '{currentFileName}'." );

				// Switch back to the metadata tab.
				homePage.TopPane.TabButtons.MetadataTabClick( documentNames[ j ] );
			}
		}

		[Test]
		[TestCase(
			"DragAndDropFile.docx",
			"DragAndDropFile",
			3,
			"Other Document" )]
		public void CreateSingleDocumentDragAndDrop(
			string fileName,
			string objectTitle,
			int expectedPageCount,
			string documentClass )
		{
			// Get the full file path by adding the file name to the folder path.
			string filePath = DragAndDropFilesFolderPath + fileName;

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to Recently Accessed by Me view.
			ListView listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Recent );

			// Drag and drop file to listing.
			MetadataCardPopout newMDCard = listing.DragAndDropFile( filePath, objectTitle );

			Assert.AreEqual( objectTitle, newMDCard.Properties.GetPropertyValue( "Name or title" ) );

			// Set class to the new document.
			newMDCard.Properties.SetPropertyValue( "Class", documentClass );

			// Save the document. Its metadata card opens to right pane.
			MetadataCardRightPane mdCard = newMDCard.SaveAndDiscardOperations.Save();

			// Open preview of the document.
			PreviewPane preview = homePage.TopPane.TabButtons.PreviewTabClick();

			// Assert that the file content is displayed in the preview and that
			// the page count is as expected.
			Assert.AreEqual( PreviewPane.PreviewStatus.ContentDisplayed, preview.Status );
			Assert.AreEqual( expectedPageCount, preview.FileContentPageCount );

		}

		// Note: Seems that dropping multiple files (by using the JavaScript workaround) works only with Chrome.
		[Test]
		[Category( "SkipFirefox" )]
		[Category( "SkipEdge" )]
		[Category( "SkipIE" )]
		[TestCase(
			"DnDMultipleDoc.docx;DnDMultipleText.txt;DnDMultipleBmp.bmp;DnDMultipleExcel.xlsx;DnDMultipleSlides.pptx",
			"3;1;1;1;4",
			"Other Document",
			4,
			"Keywords",
			"CreateMultipleDocumentsDragAndDrop" )]
		public void CreateMultipleDocumentsDragAndDrop(
			string fileNamesString,
			string previewPageCountsString,
			string documentClass,
			int numberOfClassProperties,
			string property,
			string propertyValue )
		{
			// Convert test data strings to lists.
			List<string> fileNames = fileNamesString.Split( ';' ).ToList();
			List<string> previewPageCounts = previewPageCountsString.Split( ';' ).ToList();

			// Get full file paths to the test files.
			List<string> filePaths = this.GetFilePathsOfTestFiles( fileNames );

			// Start test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to search view by using a search word that produces some results.
			ListView listing = homePage.SearchPane.QuickSearch( "power" );

			// Drag and drop multiple files and get the new metadata card of the first new document.
			MetadataCardPopout newMDCard = listing.DragAndDropFile( filePaths );

			// Set class.
			newMDCard.Properties.SetPropertyValue( "Class", documentClass, numberOfClassProperties );
			newMDCard.Properties.SetPropertyValue( property, propertyValue );

			MetadataCardRightPane mdCard = null;

			// Go through all the new documents.
			for( int i = 0; i < fileNames.Count; ++i )
			{
				// Check if the current document is not the last document yet.
				if( i < fileNames.Count - 1 )
				{
					// This is not yet the last document. Create the document and get
					// the metadata card of the next document.
					newMDCard = newMDCard.CreateAndGetNext();
				}
				else
				{
					// This is the last document. Create the document and a new
					// one should not open.
					mdCard = newMDCard.SaveAndDiscardOperations.Save();
				}
			}

			// Verify that the created objects have expected metadata and that their file content
			// is displayed in preview.
			this.VerifyMultipleCreatedDocuments(
				mdCard, homePage,
				fileNames, previewPageCounts,
				documentClass, property, propertyValue );
		}

		// Note: Seems that dropping multiple files (by using the JavaScript workaround) works only with Chrome.
		[Test]
		[Category( "SkipFirefox" )]
		[Category( "SkipEdge" )]
		[Category( "SkipIE" )]
		[TestCase(
			"DnDMultipleBat.bat;DnDMultipleExe.exe;DnDMultipleJar.jar;DnDMultipleJnlp.jnlp",
			"Other Document",
			4,
			"Keywords",
			"CreateMultipleUnsupportedFilesDragAndDrop" )]
		public void CreateMultipleUnsupportedFilesDragAndDrop(
			string fileNamesString,
			string documentClass,
			int numberOfClassProperties,
			string property,
			string propertyValue )
		{
			// Convert test data strings to lists.
			List<string> fileNames = fileNamesString.Split( ';' ).ToList();

			// Get full file paths to the test files.
			List <string> filePaths = this.GetFilePathsOfTestFiles( fileNames );

			// Start test at home page.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to search view by using a search word that produces some results.
			ListView listing = homePage.SearchPane.QuickSearch( "power" );

			// Drag and drop multiple files and get the new metadata card of the first new document.
			MetadataCardPopout newMDCard = listing.DragAndDropFile( filePaths );

			// Set class.
			newMDCard.Properties.SetPropertyValue( "Class", documentClass, numberOfClassProperties );
			newMDCard.Properties.SetPropertyValue( property, propertyValue );

			MetadataCardRightPane mdCard = null;

			// Go through all the new documents.
			for( int i = 0; i < fileNames.Count; ++i )
			{
				// Check if the current document is not the last document yet.
				if( i < fileNames.Count - 1 )
				{
					// This is not yet the last document. Create the document and get
					// the metadata card of the next document.
					newMDCard = newMDCard.CreateAndGetNext();
				}
				else
				{
					// This is the last document. Create the document and a new
					// one should not open.
					mdCard = newMDCard.SaveAndDiscardOperations.Save();
				}
			}

			// Verify that the created objects have expected metadata and that their file content
			// is displayed in preview.
			this.VerifyMultipleCreatedDocuments(
				mdCard, homePage,
				fileNames,
				documentClass, property, propertyValue );
		}

		// Note: Seems that dropping multiple files (by using the JavaScript workaround) works only with Chrome.
		[Test]
		[Category( "SkipFirefox" )]
		[Category( "SkipEdge" )]
		[Category( "SkipIE" )]
		[TestCase(
			"DnDCreateAllDoc.docx;DnDCreateAllText.txt;DnDCreateAllBmp.bmp;DnDCreateAllExcel.xlsx;DnDCreateAllSlides.pptx",
			"3;1;1;1;4",
			"Other Document",
			4,
			"Keywords",
			"CreateAllDocumentsDragAndDrop" )]
		public void CreateAllDocumentsDragAndDrop(
			string fileNamesString,
			string previewPageCountsString,
			string documentClass,
			int numberOfClassProperties,
			string property,
			string propertyValue )
		{

			// Convert test data strings to lists.
			List<string> fileNames = fileNamesString.Split( ';' ).ToList();
			List<string> previewPageCounts = previewPageCountsString.Split( ';' ).ToList();

			// Get full file paths to the test files.
			List<string> filePaths = this.GetFilePathsOfTestFiles( fileNames );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to search view by using a search word that produces some results.
			ListView listing = homePage.SearchPane.QuickSearch( "power" );

			// Drag and drop multiple files and get the new metadata card of the first new document.
			MetadataCardPopout newMDCard = listing.DragAndDropFile( filePaths );

			// Set class and a property value.
			newMDCard.Properties.SetPropertyValue( "Class", documentClass, numberOfClassProperties );
			newMDCard.Properties.SetPropertyValue( property, propertyValue );

			// Create all documents.
			MetadataCardRightPane mdCard = newMDCard.CreateAll( fileNames.Count );

			// Verify that the created objects have expected metadata and that their file content
			// is displayed in preview.
			this.VerifyMultipleCreatedDocuments(
				mdCard, homePage,
				fileNames, previewPageCounts,
				documentClass, property, propertyValue );

		}

		// Note: Seems that dropping multiple files (by using the JavaScript workaround) works only with Chrome.
		[Test]
		[Category( "SkipFirefox" )]
		[Category( "SkipEdge" )]
		[Category( "SkipIE" )]
		[TestCase(
			"DnDSkipSomeDoc.docx;DnDSkipSomeText.txt;DnDSkipSomeBmp.bmp;DnDSkipSomeExcel.xlsx;DnDSkipSomeSlides.pptx",
			"DnDSkipSomeDoc;DnDSkipSomeText;DnDSkipSomeBmp;DnDSkipSomeExcel;DnDSkipSomeSlides.pptx",
			"3;1;1;1;4",
			"Other Document",
			4,
			"Keywords",
			"SkipCreatingSomeDocumentsDragAndDrop" )]
		public void SkipCreatingSomeDocumentsDragAndDrop(
			string fileNamesString,
			string objectTitlesString,
			string previewPageCountsString,
			string documentClass,
			int numberOfClassProperties,
			string property,
			string propertyValue )
		{

			// Convert test data strings to lists.
			List<string> fileNames = fileNamesString.Split( ';' ).ToList();
			List<string> objectTitles = objectTitlesString.Split( ';' ).ToList();
			List<string> previewPageCounts = previewPageCountsString.Split( ';' ).ToList();

			// Get full file paths to the test files.
			List<string> filePaths = this.GetFilePathsOfTestFiles( fileNames );

			// List to store the names of the files that are skipped during creation.
			// The names cannot be known beforehand because the order of dragged and
			// dropped files varies.
			List<string> skippedFiles = new List<string>();

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to search view by using a search word that produces some results.
			ListView listing = homePage.SearchPane.QuickSearch( "power" );

			// Drag and drop multiple files and get the new metadata card of the first new document.
			MetadataCardPopout newMDCard = listing.DragAndDropFile( filePaths );

			// The first file will be skipped. Get its title.
			string firstSkippedObjectTitle = newMDCard.Header.Title;

			// Get index of the first object in the title list.
			int firstObjIndex = objectTitles.IndexOf( firstSkippedObjectTitle );

			// Add the file name to the skipped files list.
			skippedFiles.Add( fileNames[ firstObjIndex ] );

			// Remove that file from all lists.
			objectTitles.RemoveAt( firstObjIndex );
			fileNames.RemoveAt( firstObjIndex );
			previewPageCounts.RemoveAt( firstObjIndex );

			// Skip the first file and get next object's metadata card.
			newMDCard = newMDCard.SkipThis();

			// Set metadata of second object and create it.
			newMDCard.Properties.SetPropertyValue( "Class", documentClass, numberOfClassProperties );
			newMDCard.Properties.SetPropertyValue( property, propertyValue );
			newMDCard = newMDCard.CreateAndGetNext();

			// The 3rd object will be skipped. Get its title.
			string secondSkippedObjectTitle = newMDCard.Header.Title;

			// Also, add this file to the skipped list and remove it from other lists.
			int secondObjIndex = objectTitles.IndexOf( secondSkippedObjectTitle );
			objectTitles.RemoveAt( secondObjIndex );
			skippedFiles.Add( fileNames[ secondObjIndex ] );
			fileNames.RemoveAt( secondObjIndex );
			previewPageCounts.RemoveAt( secondObjIndex );

			// Skip the document creation.
			newMDCard = newMDCard.SkipThis();

			// 3 documents are already handled, the rest will be created by create all button.
			MetadataCardRightPane mdCard = newMDCard.CreateAll( objectTitles.Count - 3 );

			// Verify the documents that were created.
			this.VerifyMultipleCreatedDocuments(
				mdCard, homePage,
				fileNames, previewPageCounts,
				documentClass, property, propertyValue );

			// Go through the skipped files.
			foreach( string skippedFileName in skippedFiles )
			{
				// Assert that skipped files don't exist in the listing.
				Assert.False( listing.IsItemInListing( skippedFileName ),
					$"File/document '{skippedFileName}' exists in listing after it was skipped." );
			}

		}

		// Note: Seems that dropping multiple files (by using the JavaScript workaround) works only with Chrome.
		[Test]
		[Category( "SkipFirefox" )]
		[Category( "SkipEdge" )]
		[Category( "SkipIE" )]
		[TestCase(
			"DnDWorkflowExcel.xlsx;DndWorkflowSlides.pptx",
			"1;4",
			"Automation Test",
			4,
			"Keywords",
			"DocumentWithDefaultForcedWorkflowDragAndDrop",
			"Automation Test",
			"Draft" )]
		public void DocumentWithDefaultForcedWorkflowDragAndDrop(
			string fileNamesString,
			string previewPageCountsString,
			string documentClass,
			int numberOfClassProperties,
			string property,
			string propertyValue,
			string workflow,
			string workflowState )
		{

			// Convert test data strings to lists.
			List<string> fileNames = fileNamesString.Split( ';' ).ToList();
			List<string> previewPageCounts = previewPageCountsString.Split( ';' ).ToList();

			// Get full file paths to the test files.
			List<string> filePaths = this.GetFilePathsOfTestFiles( fileNames );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to search view by using a search word that produces some results.
			ListView listing = homePage.SearchPane.QuickSearch( "power" );

			// Drag and drop multiple files and get the new metadata card of the first new document.
			MetadataCardPopout newMDCard = listing.DragAndDropFile( filePaths );

			// Set class which has default forced workflow. Also set some property value.
			newMDCard.Properties.SetPropertyValue( "Class", documentClass, numberOfClassProperties );
			newMDCard.Properties.SetPropertyValue( property, propertyValue );

			MetadataCardRightPane mdCard = null;

			// Go through all the new documents.
			for( int i = 0; i < fileNames.Count; ++i )
			{
				// Check if the current document is not the last document yet.
				if( i < fileNames.Count - 1 )
				{
					// This is not yet the last document. Create the document and get
					// the metadata card of the next document.
					newMDCard = newMDCard.CreateAndGetNext();
				}
				else
				{
					// This is the last document. Create the document and a new
					// one should not open.
					mdCard = newMDCard.SaveAndDiscardOperations.Save();
				}
			}

			// Verify the documents that were created.
			this.VerifyMultipleCreatedDocuments( mdCard, homePage,
				fileNames, previewPageCounts,
				documentClass, property, propertyValue,
				workflow, workflowState );

		}


		[Test]
		[TestCase(
			"DnDCheckoutDoc.docx",
			"DnDCheckoutDoc",
			3,
			"Other Document" )]
		public void CreateCheckedOutSingleDocumentDragAndDrop(
			string fileName,
			string objectTitle,
			int expectedPageCount,
			string documentClass )
		{
			// Get the full file path by adding the file name to the folder path.
			string filePath = DragAndDropFilesFolderPath + fileName;

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to Recently Accessed by Me view.
			ListView listing = homePage.TopPane.TabButtons.ViewTabClick( TabButtons.ViewTab.Recent );

			// Drag and drop file to listing.
			MetadataCardPopout newMDCard = listing.DragAndDropFile( filePath, objectTitle );

			// Set class to the new document.
			newMDCard.Properties.SetPropertyValue( "Class", documentClass );

			// Uncheck the checkout immediately checkbox.
			newMDCard.CheckInImmediatelyClick();

			// Save the document. Its metadata card opens to right pane.
			MetadataCardRightPane mdCard = newMDCard.SaveAndDiscardOperations.Save();

			// Assert that object looks like a checked out object.
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser, listing.GetObjectCheckOutStatus( fileName ) );
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser, mdCard.Header.CheckOutStatus );

			// Open preview of the document.
			PreviewPane preview = homePage.TopPane.TabButtons.PreviewTabClick();

			// Assert that the file content is displayed in the preview and that
			// the page count is as expected.
			Assert.AreEqual( PreviewPane.PreviewStatus.ContentDisplayed, preview.Status,
				$"Preview status mismatch for object '{fileName}'." );
			Assert.AreEqual( expectedPageCount, preview.FileContentPageCount,
				$"Page count mismatch for object '{fileName}'." );

			homePage.TopPane.TabButtons.HomeTabClick();

			// Go to Checked out to me view and assert that the new object is visible there.
			homePage.ListView.GroupingHeaders.ExpandGroup( "Other Views" );
			listing = homePage.ListView.NavigateToView( "Checked Out to Me" );
			Assert.True( listing.IsItemInListing( fileName ),
				$"Just drag and drop created object '{fileName}' not found in Checked Out to Me view." );

		}

		[Test]
		[TestCase(
			"DnDProcessingDialogBmp.bmp",
			"DnDProcessingDialogBmp",
			"Other Document" )]
		public void ProcessingDialogSingleFileDragAndDrop(
			string fileName,
			string objectTitle,
			string documentClass )
		{
			// Get the full file path by adding the file name to the folder path.
			string filePath = DragAndDropFilesFolderPath + fileName;

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to search view.
			ListView listing = homePage.SearchPane.QuickSearch( "test" );

			// Variable for storing result of if the processing dialog
			// was dispayed.
			bool wasProcessingDialogDisplayed;

			// Drag and drop file. During this, checks if processing dialog was visible.
			MetadataCardPopout newMDCard = listing.DragAndDropFile( filePath, objectTitle, out wasProcessingDialogDisplayed );

			// Assert that the processing dialog should have been seen.
			Assert.True( wasProcessingDialogDisplayed,
				$"Processing dialog was not displayed when drag and dropped file '{fileName}'." );

			// Set class and create the object.
			newMDCard.Properties.SetPropertyValue( "Class", documentClass );
			newMDCard.SaveAndDiscardOperations.Save();
		}

		// Note: Seems that dropping multiple files (by using the JavaScript workaround) works only with Chrome.
		[Test]
		[Category( "SkipFirefox" )]
		[Category( "SkipEdge" )]
		[Category( "SkipIE" )]
		[TestCase(
			"DnDProcessingDialogMultiBmp.bmp;DnDProcessingDialogMultiSlides.pptx",
			"Other Document" )]
		public void ProcessingDialogMultipleFilesDragAndDrop(
			string fileNamesString,
			string documentClass )
		{

			// Convert test data strings to lists.
			List<string> fileNames = fileNamesString.Split( ';' ).ToList();

			// Get full file paths to the test files.
			List<string> filePaths = this.GetFilePathsOfTestFiles( fileNames );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to search view.
			ListView listing = homePage.SearchPane.QuickSearch( "test" );

			// Variable for storing result of if the processing dialog
			// was dispayed.
			bool wasProcessingDialogDisplayed;

			// Drag and drop file. During this, checks if processing dialog was visible.
			MetadataCardPopout newMDCard = listing.DragAndDropFile( filePaths, out wasProcessingDialogDisplayed );

			// Assert that the processing dialog should have been seen.
			Assert.True( wasProcessingDialogDisplayed,
				$"Processing dialog was not displayed when drag and dropped multiple files." );

			// Set class and create the object.
			newMDCard.Properties.SetPropertyValue( "Class", documentClass );
			newMDCard.CreateAll( fileNames.Count );
		}
	}
}
