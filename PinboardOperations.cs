using System;
using System.Collections.Generic;
using System.Linq;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.Listing;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	// TODO: This class takes more time than 6 if running the MaxNumberOfPinnedItems test case.
	[Order( -6 )]
	[Parallelizable( ParallelScope.Self )]
	class PinboardOperations
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		protected string username;
		protected string password;
		protected string vaultName;

		protected TestClassConfiguration configuration;

		protected MFilesContext mfContext;

		protected TestClassBrowserManager browserManager;

		// Additional assertion messages variables.
		private readonly string BreadCrumbItemsMismatch = "Mismatch between the expected and actual breadcrumb items.";
		private readonly string PinnedItemPresenceInPinboardMismatch = "Mismatch between the expected and actual item '{0}' presence in the pinboard.";
		private readonly string ExpectecItemPresenceInListViewMismatch = "Mismatch between the expected and actual item '{0}' presence in the list view.";

		public PinboardOperations()
		{
			this.classID = "PinboardOperations";
		}

		[OneTimeSetUp]
		public virtual void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetTestUsers( 4 );

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Data Types And Test Objects.mfb", users );

			this.vaultName = this.mfContext.VaultName;

			// TODO: The "user" identifier here is now defined in SetupHelper. Maybe this should come from configuration and
			// it should also be given to the SetupHelper as parameter.
			this.username = this.mfContext.UsernameOfUser( "user1" );
			this.password = this.mfContext.PasswordOfUser( "user1" );

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
		/// Pin an object to pinboard. Then use pinboard to navigate to view the object. Finally, unpin the object
		/// from pinboard.
		/// </summary>
		[Test]
		[TestCase(
			"Bill of Materials: Doors.xls",
			"Document",
			"Bill of Materials: Doors",
			Description = "Single file document object.",
			Category = "Smoke" )]
		[TestCase(
			"ERP Training - Day 1.ppt",
			"Document",
			"ERP Training - Day 1",
			Description = "PPT file type.",
			Category = "Pinboard" )]
		[TestCase(
			"WorkflowStateTransitionWithActionInDocxFileType.docx",
			"Document",
			"WorkflowStateTransitionWithActionInDocxFileType",
			Description = "Docx file type.",
			Category = "Pinboard" )]
		[TestCase(
			"View from the Sea.jpg",
			"Document",
			"View from the Sea",
			Description = "JPG file type.",
			Category = "Pinboard" )]
		[TestCase(
			"First Mountain Securities (Customer Service)",
			"Customer",
			"First Mountain Securities (Customer Service)",
			Description = "Non-document object without attached files.",
			Category = "Smoke" )]
		public virtual void PinAndUnpinObject(
			string objectName,
			string objectType,
			string objectTitle,
			string breadCrumbItems = "" )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( objectName );

			Pinboard pinboard = contextMenu.Pin();

			// Navigate to the pinned object. This also automatically opens its metadata card.
			MetadataCardRightPane mdCard = pinboard.ClickPinnedObject( objectTitle );

			// Declare the variable to store the breadcrumb items with vault name as the first value.
			List<string> expectedBreadCrumbItems = new List<string> { this.vaultName };

			// Add items in breadcrumb list if breadCrumbItems is not empty.
			if( !breadCrumbItems.Equals( "" ) )
				foreach( string item in breadCrumbItems.Split( '>' ) )
				{
					expectedBreadCrumbItems.Add( item );
				}

			// Assert that breadcrumb displays the vault name.
			Assert.AreEqual( expectedBreadCrumbItems, homePage.TopPane.BreadCrumb, BreadCrumbItemsMismatch );

			//Unpin the object from list view.
			contextMenu = listing.RightClickItemOpenContextMenu( objectName );

			contextMenu.Unpin();

			pinboard = homePage.TopPane.TabButtons.PinnedTabClick();

			// Assert that the object was removed from pinboard.
			Assert.False( pinboard.IsItemInPinboard( objectTitle ), string.Format( PinnedItemPresenceInPinboardMismatch, objectTitle ) );

		}

		/// <summary>
		/// Pin a view to pinboard. Then use pinboard to navigate to the view. Finally, unpin the view
		/// from pinboard.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"1. Documents",
			"By Class",
			"Agenda",
			Description = "View inside a view." )]
		[TestCase(
			"1. Documents>By Customer",
			"Davis & Cobb, Attorneys at Law",
			"Sales Invoice 250 - Davis & Cobb, Attorneys at Law.xls",
			Description = "Virtual folder." )]
		public virtual void PinAndUnpinViewInsideAnotherView(
			string viewPath,
			string viewItem,
			string controlItem )
		{
			List<string> viewSteps = StringSplitHelper.ParseStringToStringList( viewPath, '>' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Got to a view.
			ListView listing = homePage.ListView.NavigateToView( viewPath );

			// Select a view or virtual folder inside the view and pin it to pinboard.
			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( viewItem, waitForMetadataCard: false );
			Pinboard pinboard = contextMenu.Pin();

			// Navigate to the view by using the pinboard.
			listing = pinboard.ClickPinnedView( viewItem );

			// Declare the variable to store the breadcrumb items with vault name as the first value.
			List<string> expectedBreadCrumbItems = new List<string> { this.vaultName };

			// Add the view paths.
			foreach( string viewStep in viewSteps )
			{
				expectedBreadCrumbItems.Add( viewStep );
			}

			// Add the pinned view item.
			expectedBreadCrumbItems.Add( viewItem );

			// Assert that breadcrumb displays the vault name.
			Assert.AreEqual( expectedBreadCrumbItems, homePage.TopPane.BreadCrumb, BreadCrumbItemsMismatch );

			// Assert that some expected object/virtual folder is displayed in the view.
			Assert.True( listing.IsItemInListing( controlItem ), string.Format( ExpectecItemPresenceInListViewMismatch, controlItem ) );

			// Go back one level from the current view. In other words return to the view where
			// the pinned view/virtual folder is listed.
			listing = homePage.TopPane.NavigateToBreadCrumbItem( viewSteps.ElementAt( viewSteps.Count - 1 ) );

			contextMenu = listing.RightClickItemOpenContextMenu( viewItem, waitForMetadataCard: false );
			contextMenu.Unpin();

			pinboard = homePage.TopPane.TabButtons.PinnedTabClick();

			// Assert that the view was removed from the pinboard.
			Assert.False( pinboard.IsItemInPinboard( viewItem ), string.Format( PinnedItemPresenceInPinboardMismatch, viewItem ) );
		}

		[Test]
		[Category( "Smoke" )]
		[TestCase( "2. Manage Customers", "ESTT Corporation (IT)" )]
		public virtual void PinAndUnpinViewInHomeView( string view, string controlItem )
		{

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListViewItemContextMenu contextMenu = homePage.ListView.RightClickItemOpenContextMenu( view, waitForMetadataCard: false );

			Pinboard pinboard = contextMenu.Pin();

			// Navigate to the pinned view.
			ListView listing = pinboard.ClickPinnedView( view );

			// Assert that breadcrumb displays the vault name.
			Assert.AreEqual( new List<string> { this.vaultName, view }, homePage.TopPane.BreadCrumb, BreadCrumbItemsMismatch );

			// Assert that some expected item is displayed in the view.
			Assert.True( listing.IsItemInListing( controlItem ), string.Format( ExpectecItemPresenceInListViewMismatch, controlItem ) );

			// Go back to home view.
			homePage = homePage.TopPane.TabButtons.HomeTabClick();

			contextMenu = homePage.ListView.RightClickItemOpenContextMenu( view, waitForMetadataCard: false );
			contextMenu.Unpin();

			pinboard = homePage.TopPane.TabButtons.PinnedTabClick();

			// Assert that the view was removed from pinboard.
			Assert.False( pinboard.IsItemInPinboard( view ), string.Format( PinnedItemPresenceInPinboardMismatch, view ) );
		}

		/// <summary>
		/// Unpin an object by opening the context menu directly in the pinboard.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Income Statement 1/2007.xls", "Document", "Income Statement 1/2007" )]
		public virtual void UnpinObjectFromPinboard( string objectName, string objectType, string objectTitle )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( objectName );

			Pinboard pinboard = contextMenu.Pin();

			// Assert that object appeared to pinboard.
			Assert.True( pinboard.IsItemInPinboard( objectTitle ), string.Format( PinnedItemPresenceInPinboardMismatch, objectTitle ) );

			// Unpin the object by using context menu in pinboard.
			pinboard.UnpinItem( objectTitle );

			// Assert that object was removed from  pinboard.
			Assert.False( pinboard.IsItemInPinboard( objectTitle ), string.Format( PinnedItemPresenceInPinboardMismatch, objectTitle ) );
		}

		/// <summary>
		/// Add multiple objects to pinboard and then navigate to them by using the pinboard.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Order - RMP.doc;Training Slides;Project Announcement / Austin.pdf",
			"Order - RMP;Training Slides;Project Announcement / Austin",
			"Document;Document collection;Document" )]
		public virtual void PinMultipleObjects(
			string objects,
			string objectTitles,
			string objectTypes )
		{
			List<string> objectStrings = StringSplitHelper.ParseStringToStringList( objects, ';' );
			List<string> objectTitleStrings = StringSplitHelper.ParseStringToStringList( objectTitles, ';' );
			List<string> objectTypeStrings = StringSplitHelper.ParseStringToStringList( objectTypes, ';' );


			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go through test objects and add the to pinboard.
			for( int i = 0; i < objectStrings.Count; ++i )
			{
				string objectName = objectStrings.ElementAt( i );
				string objectType = objectTypeStrings.ElementAt( i );

				// Search for the object.
				ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

				ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( objectName );
				contextMenu.Pin();

				// Return to home page.
				homePage.TopPane.TabButtons.HomeTabClick();
			}

			// After all objects are added to pinboard, then view the pinboard.
			Pinboard pinboard = homePage.TopPane.TabButtons.PinnedTabClick();

			// Go through all the added pinned objects.
			for( int j = 0; j < objectStrings.Count; ++j )
			{
				string objectName = objectStrings.ElementAt( j );
				string objectTitle = objectTitleStrings.ElementAt( j );

				// Clicking a pinned object in pinboard opens its metadata card automatically to right pane.
				MetadataCardRightPane mdCard = pinboard.ClickPinnedObject( objectTitle );

				Assert.True( homePage.ListView.IsItemInListing( objectName ) );

				pinboard = homePage.TopPane.TabButtons.PinnedTabClick();
			}

		}

		/// <summary>
		/// Add multiple views/virtual folders to pinboard and then navigate to them by using pinboard.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"1. Documents;1. Documents>By Class;5. Advanced Sample Views>Invoices by Year and Month",
			"By Project;Picture;2007-02",
			"Central Plains Area Development;River.jpg;Invoice #656 - UPP Consulting.pdf" )]
		public virtual void PinMultipleViewsAndVirtualFolders(
			string viewPaths,
			string viewItems,
			string controlItems )
		{
			// This list contains all view paths where user navigates.
			List<string> viewPathStrings = StringSplitHelper.ParseStringToStringList( viewPaths, ';' );

			// This list contains the actual views/virtual folders that will be pinned when the view
			// path is reached.
			List<string> viewItemStrings = StringSplitHelper.ParseStringToStringList( viewItems, ';' );

			// This test data will contain one control item for each view. Control items are used to
			// verify that the navigation to the view has succeeded because the view contains that item.
			List<string> controlItemStrings = StringSplitHelper.ParseStringToStringList( controlItems, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go through views to add them to pinboard.
			for( int i = 0; i < viewItemStrings.Count; ++i )
			{
				string viewPath = viewPathStrings.ElementAt( i );
				string viewItem = viewItemStrings.ElementAt( i );

				// Go to view path.
				ListView listing = homePage.ListView.NavigateToView( viewPath );

				// Add view or virtual folder to pinboard.
				ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( viewItem, waitForMetadataCard: false );
				contextMenu.Pin();

				// Return to home page after adding a view to pinboard.
				homePage = homePage.TopPane.TabButtons.HomeTabClick();
			}

			// After all views are added to pinboard, then view the pinboard.
			Pinboard pinboard = homePage.TopPane.TabButtons.PinnedTabClick();

			// Go through all pinned views.
			for( int j = 0; j < viewItemStrings.Count; ++j )
			{
				string viewItem = viewItemStrings.ElementAt( j );
				string controlItem = controlItemStrings.ElementAt( j );

				// Navigate to pinned view by using pinboard.
				ListView viewListing = pinboard.ClickPinnedView( viewItem );

				// Assert that expected control item is in listing.
				Assert.True( viewListing.IsItemInListing( controlItem ), string.Format( ExpectecItemPresenceInListViewMismatch, controlItem ) );
			}
		}

		/// <summary>
		/// Pin an object that has attached files. Then use pinboard to navigate to the object.
		/// The object's relationships should be expanded by default.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Training Plan",
			"Document",
			"Training Plan.doc;Training Plan.pdf",
			"Projects:IT Training" )]
		public virtual void PinnedObjectWithFilesHasExpandedRelationships(
			string objectName,
			string objectType,
			string attachedFiles,
			string relatedObjects )
		{
			List<string> attachedFileStrings = StringSplitHelper.ParseStringToStringList( attachedFiles, ';' );

			// Parse test data string into a dictionary where related objects are stored as list of strings
			// with relationships header as the key.
			Dictionary<string, List<string>> expectedRelatedObjectsByHeader =
				StringSplitHelper.ParseStringToStringListsByKey( relatedObjects, ';', ':', '|' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( objectName );

			Pinboard pinboard = contextMenu.Pin();

			// Navigate to the pinned object by using the pinboard.
			MetadataCardRightPane mdCard = pinboard.ClickPinnedObject( objectName );

			// Get relationships of the object. Note that calling method to wait until loaded here because the 
			// pinned object has attached files and thus its relationships are automatically expanded.
			RelationshipsTree relationships = listing.GetRelationshipsTreeOfObject( objectName ).WaitUntilLoaded();

			// Assert that the relationships are already expanded without having to expand them.
			Assert.AreEqual( RelationshipsTreeStatus.Expanded, relationships.Status );

			// Check that all attached files are visible.
			foreach( string attachedFile in attachedFileStrings )
			{
				Assert.True( relationships.IsFileAttached( attachedFile ) );
			}

			// Go through all relationship headers.
			foreach( string relationshipHeader in expectedRelatedObjectsByHeader.Keys )
			{
				relationships.ExpandRelationshipHeader( relationshipHeader );

				// Go through all related objects under this header.
				foreach( string relatedObject in expectedRelatedObjectsByHeader[ relationshipHeader ] )
				{
					Assert.True( relationships.IsObjectInRelationships( relationshipHeader, relatedObject ),
						String.Format( "Expected object '{0}' is not listed in relationships of object '{1}' under header '{2}'",
						relatedObject, objectName, relationshipHeader ) );
				}
			}
		}


		/// <summary>
		/// Testing that the re-arranged positions of the pinned object is retained.
		/// </summary>
		[Test]
		[Category( "Pinboard" )]
		[Order( 1 )]
		[TestCase(
			"Project",
			"Redesign of ESTT Marketing Material;Sales Strategy Development;Office Design;Austin District Redevelopment",
			"Sales Strategy Development",
			"Office Design" )]
		public virtual void CheckRearrangedPinnedObjectPositionIsRetained(
			string objectType,
			string pinItems,
			string dragItem,
			string toItem )
		{
			// Start the test at home page by using different user.
			// This way the pinned object positions are not dependent on any
			// previous tests.
			HomePage homePage = browserManager.FreshLoginAndStartTestAtHomePage(
				this.mfContext.UsernameOfUser( "user2" ),
				this.mfContext.PasswordOfUser( "user2" ),
				this.vaultName );

			// Navigate to the view.
			ListView listing = listing = homePage.SearchPane.FilteredQuickSearch( "", objectType );

			// Pin some objects.
			foreach( string pinItem in pinItems.Split( ';' ) )
			{
				// Add the items to pin board.
				listing.RightClickItemOpenContextMenu( pinItem ).Pin();
			}

			// Click the pinned tab.
			Pinboard pinboard = homePage.TopPane.TabButtons.PinnedTabClick();

			// Get the item positions before drag the item.
			string dragItemPositionBeforeDrag = pinboard.GetPinnedItemPosition( dragItem );
			string toItemPositionBeforeDrag = pinboard.GetPinnedItemPosition( toItem );

			// Rearrange the item in pinboard.
			pinboard.RearrangeItemInPinboard( dragItem, toItem );

			// Get the item positions after drag the item.
			string dragItemPositionAfterDrag = pinboard.GetPinnedItemPosition( dragItem );
			string toItemPositionAfterDrag = pinboard.GetPinnedItemPosition( toItem );

			// Assert that the item moved away when other item was dragged on top of it.
			Assert.AreNotEqual( toItemPositionBeforeDrag, toItemPositionAfterDrag,
				$"Pinned item '{toItem}' position is same before and after drag the item '{dragItem}' in pinboard." );

			// Assert that the item moved to take the place of the other item.
			Assert.AreEqual( toItemPositionBeforeDrag, dragItemPositionAfterDrag,
				$"Item '{dragItem}' was not dragged to the place of item '{toItem}'." );

			// Navigate to home view.
			homePage = homePage.TopPane.TabButtons.HomeTabClick();

			// Get the pinboard instance.
			pinboard = homePage.TopPane.TabButtons.PinnedTabClick();

			// Assert that item position is not changed after navigating to some other view.
			Assert.AreEqual( dragItemPositionAfterDrag, pinboard.GetPinnedItemPosition( dragItem ),
				"Mismatch between the expected and actual position of the dragged item '" + dragItem + "' in the pinboard." );

			this.browserManager.EnsureQuitBrowser();
		}

		/// <summary>
		/// Testing that the re-arranged positions of the pinned view is retained.
		/// </summary>
		[Test]
		[Category( "Pinboard" )]
		[Order( 1 )]
		[TestCase(
			"1. Documents",
			"Pending Proposals;Purchase Invoices;By Project and Class",
			"By Project and Class",
			"Pending Proposals" )]
		public virtual void CheckRearrangedPinnedViewPositionIsRetained(
			string viewToNavigate,
			string pinItems,
			string dragItem,
			string toItem )
		{
			// Start the test at home page by using different user.
			// This way the pinned object positions are not dependent on any
			// previous tests.
			HomePage homePage = browserManager.FreshLoginAndStartTestAtHomePage(
				this.mfContext.UsernameOfUser( "user3" ),
				this.mfContext.PasswordOfUser( "user3" ),
				this.vaultName );

			// Navigate to the view.
			ListView listing = listing = homePage.ListView.NavigateToView( viewToNavigate );

			// Pin some objects.
			foreach( string pinItem in pinItems.Split( ';' ) )
			{
				// Add the items to pin board.
				listing.RightClickItemOpenContextMenu( pinItem, waitForMetadataCard: false ).Pin();
			}

			// Click the pinned tab.
			Pinboard pinboard = homePage.TopPane.TabButtons.PinnedTabClick();

			// Get the item positions before drag the item.
			string dragItemPositionBeforeDrag = pinboard.GetPinnedItemPosition( dragItem );
			string toItemPositionBeforeDrag = pinboard.GetPinnedItemPosition( toItem );

			// Rearrange the item in pinboard.
			pinboard.RearrangeItemInPinboard( dragItem, toItem );

			// Get the item positions after drag the item.
			string dragItemPositionAfterDrag = pinboard.GetPinnedItemPosition( dragItem );
			string toItemPositionAfterDrag = pinboard.GetPinnedItemPosition( toItem );

			// Assert that the item moved away when other item was dragged on top of it.
			Assert.AreNotEqual( toItemPositionBeforeDrag, toItemPositionAfterDrag,
				$"Pinned item '{toItem}' position is same before and after drag the item '{dragItem}' in pinboard." );

			// Assert that the item moved to take the place of the other item.
			Assert.AreEqual( toItemPositionBeforeDrag, dragItemPositionAfterDrag,
				$"Item '{dragItem}' was not dragged to the place of item '{toItem}'." );

			// Navigate to home view.
			homePage = homePage.TopPane.TabButtons.HomeTabClick();

			// Get the pinboard instance.
			pinboard = homePage.TopPane.TabButtons.PinnedTabClick();

			// Assert that item position is not changed after navigating to some other view.
			Assert.AreEqual( dragItemPositionAfterDrag, pinboard.GetPinnedItemPosition( dragItem ),
				"Mismatch between the expected and actual position of the dragged item '" + dragItem + "' in the pinboard." );

			this.browserManager.EnsureQuitBrowser();
		}

		/// <summary>
		/// Testing that warning message is displayed when adding excessive item in the pinboard.
		/// [Maximum 100 items can be pinned.]
		/// </summary>		
		[Ignore( "TODO: Make test duration shorter." )]
		[Test]
		[Category( "Pinboard" )]
		[Order( 1 )]
		[TestCase(
			"There is no room to pin this item. You must first unpin something from the Pinned tab if you want to pin this item.",
			"Confirmation of Order - Web Graphics" )]
		public virtual void MaxNumberOfPinnedItems(
			string expectedMessage,
			string excessiveItem )
		{
			// Start the test with fresh user to avoid the existing pinned items in the pinboard.
			HomePage homePage = browserManager.FreshLoginAndStartTestAtHomePage(
				this.mfContext.UsernameOfUser( "user4" ),
				this.mfContext.PasswordOfUser( "user4" ),
				this.mfContext.VaultName );

			// Search for the object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( "", "Document" );

			// Get the list view items.
			List<string> items = listing.ItemNames.Distinct().ToList();

			// Assert that maximum number of items available to pin.
			Assert.Greater( items.Count, 100,
				"Mismatch between the maximum number of objects expected and actual objects listed in the list view to add it in pin board." );

			// Pin first item in the pinboard.
			try
			{
				// Add the item to pin board with metadata card loading wait.			
				listing.RightClickItemOpenContextMenu( items[ 0 ], waitForMetadataCard: true ).Pin();
			}
			catch( Exception ex )
			{
				throw new Exception( "Exception occurred while pinning first item '" + items[ 0 ] + "' in pinboard from the list view.", ex );
			}

			// Pin remaining 99 items in the pinboard.
			for( int i = 1; i < 100; i++ )
			{
				try
				{
					// Add the items to pin board. Skip waiting for metadata card, so that the test
					// doesn't take too much time.			
					listing.RightClickItemOpenContextMenu( items[ i ], waitForMetadataCard: false ).Pin();
				}
				catch( Exception ex )
				{
					throw new Exception( "Exception occurred while pinning '" + ( i + 1 ) + "' item '" + items[ i ] + "' in pinboard from the list view.", ex );
				}
			}

			// Get the pinboard instance.
			Pinboard pinboard = homePage.TopPane.TabButtons.PinnedTabClick();

			// Wait till 100 items pinned and loaded in Pinboard with time interval of 30 seconds. 
			pinboard.WaitUntilExpectedNumberOfPinnedItemsLoaded( 100, 30 );

			// Add the excessive item to the pinboard.
			ListViewItemContextMenu contextMenu = listing.RightClickItemOpenContextMenu( excessiveItem, waitForMetadataCard: false );
			MessageBoxDialog msgBox = contextMenu.PinAndWaitForMessageBoxDialog();

			// Assert that expected warning message is displayed when pinning the excess item in the pin board,
			Assert.AreEqual( expectedMessage, msgBox.Message,
				"Mismatch between the expected and actual warning message when adding more number of items in pinboard." );

			// Quit the browser.
			this.browserManager.EnsureQuitBrowser();
		}

	}
}