using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.Listing;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -7 )]
	[Parallelizable( ParallelScope.Self )]
	[Category( "UI" )]
	class HomeGrid
	{

		private static readonly string GroupingHeadersMismatchMessage =
			"Mismatch between expected and actual grouping headers in list view.";
		private static readonly string ItemsMismatchMessage =
			"Mismatch between expected and actual items in list view.";
		private static readonly string EmptyGridHeaderMismatchMessage =
			"Mismatch between expected and actual header text for empty grid.";
		private static readonly string EmptyGridDescriptionMismatchMessage =
			"Mismatch between expected and actual description text for empty grid";
		private static readonly string ItemCountMismatchMessage =
			"Mismatch between expected and actual item count in list view.";
		private static readonly string GridNotSelectedErrorMessage =
			"Expected grid to not be selected but it was selected.";
		private static readonly string GridSelectedErrorMessage =
			"Expected grid to be selected but it was not selected.";
		private static readonly string ItemNotFoundMessage =
			"Expected item '{0}' is not found in listing.";
		private static readonly string ItemFoundMessage =
			"Item '{0}' is found in listing when it was not expected.";

		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected string classID => "HomeGrid";

		private string username;
		private string password;
		private string vaultName;

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		public HomeGrid()
		{
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetTestUsers( 5 );

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Data Types And Test Objects.mfb", users );

			this.vaultName = this.mfContext.VaultName;

			// TODO: The "user" identifier here is now defined in SetupHelper. Maybe this should come from configuration and
			// it should also be given to the SetupHelper as parameter.
			this.username = this.mfContext.UsernameOfUser( "user1" );
			this.password = this.mfContext.PasswordOfUser( "user1" );

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

		private string FormatItemNotFoundAssertionMessage( string itemName )
		{
			return string.Format( ItemNotFoundMessage, itemName );
		}

		private string FormatItemFoundAssertionMessage( string itemName )
		{
			return string.Format( ItemFoundMessage, itemName );
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

		[Test]
		[TestCase(
			"1. Documents;2. Manage Customers;3. Manage Projects;4. Manage Employees;5. Advanced Sample Views;Templates;Assigned to Me;Checked Out to Me;Favorites;Recently Accessed by Me;Recently Modified by Me",
			 "Common Views;Other Views" )]
		public void ExpandBrowseGridMiniView(
			string viewsString,
			string groupingHeadersString )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			List<string> views = viewsString.Split( ';' ).ToList();
			List<string> groupingHeaders = groupingHeadersString.Split( ';' ).ToList();

			ListView browseGridListing = homePage.BrowseGrid;

			// Assert that the expected grouping headers are displayed.
			Assert.AreEqual( groupingHeaders, browseGridListing.GroupingHeaders.GroupTitles,
				GroupingHeadersMismatchMessage );

			// Go through grouping headers.
			foreach( string header in groupingHeaders )
			{
				// Check is the header collapsed.
				if( browseGridListing.GroupingHeaders.GetGroupStatus( header ) == GroupingHeadersInListView.GroupStatus.Collapsed )
				{
					// Expand collapsed header.
					browseGridListing.GroupingHeaders.ExpandGroup( header );
				}
			}

			// Assert that all expected views are displayed.
			Assert.AreEqual( views, browseGridListing.ItemNames, ItemsMismatchMessage );

			// Expand/navigate to the grid.
			ListView listing = browseGridListing.ExpandGrid();

			// Assert that the same grouping headers are visible in the view.
			Assert.AreEqual( groupingHeaders, listing.GroupingHeaders.GroupTitles );

			// Go through grouping headers.
			foreach( string header in groupingHeaders )
			{
				// Check is the header collapsed.
				if( listing.GroupingHeaders.GetGroupStatus( header ) == GroupingHeadersInListView.GroupStatus.Collapsed )
				{
					// Expand collapsed header.
					listing.GroupingHeaders.ExpandGroup( header );
				}
			}

			// Assert that the same views are displayed.
			Assert.AreEqual( views, listing.ItemNames, ItemsMismatchMessage );
		}

		[Test]
		[Order( 1 )]
		[TestCase(
			"Bill of Materials: Furniture;View from the Sea.jpg;Floor Plans / Central Plains;Warwick Systems & Technology;Staff Training / ERP",
			"Document;Document;Document collection;Customer;Project",
			"Keywords;Keywords;Keywords;City;In progress",
			"grid test;grid test;grid test;Tampere;Yes",
			"Documents;Document collections;Customers;Projects" )]
		public void ExpandRecentlyAccessedByMeGridMiniView(
			string objectNamesString,
			string objectTypesString,
			string propertiesString,
			string valuesString,
			string objectTypeHeadersString )
		{

			List<string> objectNames = objectNamesString.Split( ';' ).ToList();
			List<string> objectTypes = objectTypesString.Split( ';' ).ToList();
			List<string> properties = propertiesString.Split( ';' ).ToList();
			List<string> values = valuesString.Split( ';' ).ToList();
			List<string> objectTypeHeaders = objectTypeHeadersString.Split( ';' ).ToList();

			// Login as a fresh user to ensure that Recently accessed by me view is empty.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage(
				this.mfContext.UsernameOfUser( "user2" ), this.mfContext.PasswordOfUser( "user2" ), this.vaultName );

			ListView listing = null;

			// Go through test objects.
			for( int i = 0; i < objectNames.Count; ++i )
			{
				// Modify metadata of object and save. This adds them to 
				// recently accessed by me view.
				string objectName = objectNames[ i ];
				listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectTypes[ i ] );
				MetadataCardRightPane mdCard = listing.SelectObject( objectNames[ i ] );
				mdCard.Properties.SetPropertyValue( properties[ i ], values[ i ] );
				mdCard.SaveAndDiscardOperations.Save();

				// Go back to home view.
				homePage.TopPane.TabButtons.HomeTabClick();
			}

			// Assert that only the modified objects are visible in Recently accessed by me grid.
			Assert.That( homePage.RecentlyAccessedByMeGrid.ItemNames, Is.EquivalentTo( objectNames ),
				ItemsMismatchMessage );

			// Expand the grid/navigate to it.
			listing = homePage.RecentlyAccessedByMeGrid.ExpandGrid();

			// Assert that the view has grouping by object type.
			Assert.AreEqual( objectTypeHeaders, listing.GroupingHeaders.GroupTitles,
				GroupingHeadersMismatchMessage );

			// Go through grouping headers.
			foreach( string header in objectTypeHeaders )
			{
				// Check is the header collapsed.
				if( listing.GroupingHeaders.GetGroupStatus( header ) == GroupingHeadersInListView.GroupStatus.Collapsed )
				{
					// Expand collapsed header.
					listing.GroupingHeaders.ExpandGroup( header );
				}
			}

			// Assert that the same objects are visible in the expanded grid view.
			Assert.That( listing.ItemNames, Is.EquivalentTo( objectNames ),
				ItemsMismatchMessage );

			// Quit browser because the test uses specific user.
			this.browserManager.EnsureQuitBrowser();

		}

		[Test]
		[Order( 1 )]
		[TestCase(
			"Proposal 7728 - Reece, Murphy and Partners.doc;OMCC Corporation;Philo District Redevelopment",
			"Document;Customer;Project",
			"Documents;Customers;Projects" )]
		public void ExpandCheckedOutToMeGridMiniView(
			string objectNamesString,
			string objectTypesString,
			string pluralNamesString )
		{

			List<string> objectNames = objectNamesString.Split( ';' ).ToList();
			List<string> objectTypes = objectTypesString.Split( ';' ).ToList();
			List<string> pluralNames = pluralNamesString.Split( ';' ).ToList();

			// Login as a fresh user to ensure that Checked out to me view is empty.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage(
				this.mfContext.UsernameOfUser( "user3" ), this.mfContext.PasswordOfUser( "user3" ), this.vaultName );

			ListView listing = null;

			// Go through test objects.
			for( int i = 0; i < objectNames.Count; ++i )
			{
				// Search for an object and check it out.
				string objectName = objectNames[ i ];
				listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectTypes[ i ] );
				listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();
				homePage.TopPane.TabButtons.HomeTabClick();
			}

			// Assert that all objects are in the checked out to me grid.
			Assert.That( homePage.CheckedOutToMeGrid.ItemNames, Is.EquivalentTo( objectNames ),
				ItemsMismatchMessage );

			// Expand/navigate to the grid view.
			listing = homePage.CheckedOutToMeGrid.ExpandGrid();

			// Assert that the view has all checked out object types as group headers.
			Assert.AreEqual( pluralNames, listing.GroupingHeaders.GroupTitles,
				GroupingHeadersMismatchMessage );

			// Expand all collapsed object type groups.
			foreach( string pluralName in pluralNames )
			{
				if( listing.GroupingHeaders.GetGroupStatus( pluralName ) 
					== GroupingHeadersInListView.GroupStatus.Collapsed )
				{
					listing.GroupingHeaders.ExpandGroup( pluralName );
				}
			}

			// Assert that the same objects are visible there.
			Assert.That( listing.ItemNames, Is.EquivalentTo( objectNames ),
				ItemsMismatchMessage );

			// Quit browser because the test uses specific user.
			this.browserManager.EnsureQuitBrowser();

		}

		[Test]
		[Order( 1 )]
		public void ExpandAssignedToMeGridMiniView()
		{
			// Login as a fresh user to ensure that Assigned to me view is empty.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage(
				this.mfContext.UsernameOfUser( "user4" ), this.mfContext.PasswordOfUser( "user4" ), this.vaultName );

			// Names for assignments to be created.
			List<string> assignmentNames = new List<string> { "AssignmentGrid1", "AssignmentGrid2" };

			// Go through the assignments.
			foreach( string assignmentName in assignmentNames )
			{
				// Crate assignments and assign it to current user.
				MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( "Assignment" );
				newMDCard.Properties.SetPropertyValue( "Class", "Assignment", 5 );
				newMDCard.Properties.SetPropertyValue( "Assigned to", this.mfContext.UsernameOfUser( "user4" ) );
				newMDCard.Properties.SetPropertyValue( "Name or title", assignmentName );
				newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );

				// Wait until the created assignment appears to the grid.
				homePage.AssignedToMeGrid.WaitUntilItemIsInListing( assignmentName );
			}

			// Assert that the assignments are visible in the Assigned to me grid.
			Assert.That( homePage.AssignedToMeGrid.ItemNames, Is.EquivalentTo( assignmentNames ),
				ItemsMismatchMessage );

			// Expand/navigate to the view.
			ListView listing = homePage.AssignedToMeGrid.ExpandGrid();

			// Assert that the same objects are visible there.
			Assert.That( listing.ItemNames, Is.EquivalentTo( assignmentNames ),
				ItemsMismatchMessage );

			// Assert that the view has only the grouping header "Assignments".
			List<string> expectedGroupingHeaders = new List<string> { "Assignments" };
			Assert.AreEqual( expectedGroupingHeaders, listing.GroupingHeaders.GroupTitles,
				GroupingHeadersMismatchMessage );

			// Quit browser because the test uses specific user.
			this.browserManager.EnsureQuitBrowser();
		}

		[Test]
		[Order( 1 )]
		public void EmptyGridMiniViews()
		{
			// Login as a fresh user to ensure that grid views are empty.
			HomePage homePage = this.browserManager.FreshLoginAndStartTestAtHomePage(
				this.mfContext.UsernameOfUser( "user5" ), this.mfContext.PasswordOfUser( "user5" ), this.vaultName );

			// Assert that empty "Recently Accessed by Me" view displays correct text.
			Assert.AreEqual( "Recently Accessed by Me",
				homePage.RecentlyAccessedByMeGrid.EmptyGridHeaderText,
				EmptyGridHeaderMismatchMessage );
			Assert.AreEqual( "The objects you have recently accessed or edited are shown in this area.",
				homePage.RecentlyAccessedByMeGrid.EmptyGridDescriptionText,
				EmptyGridDescriptionMismatchMessage );
			Assert.AreEqual( 0, homePage.RecentlyAccessedByMeGrid.NumberOfItems,
				ItemCountMismatchMessage );

			// Assert that empty "Assigned to Me" view displays correct text.
			Assert.AreEqual( "Assigned to Me",
				homePage.AssignedToMeGrid.EmptyGridHeaderText,
				EmptyGridHeaderMismatchMessage );
			Assert.AreEqual( "The tasks assigned to you are shown in this area.",
				homePage.AssignedToMeGrid.EmptyGridDescriptionText,
				EmptyGridDescriptionMismatchMessage );
			Assert.AreEqual( 0, homePage.AssignedToMeGrid.NumberOfItems,
				ItemCountMismatchMessage );

			// Assert that empty "Checked Out to Me" view displays correct text.
			Assert.AreEqual( "Checked Out to Me",
				homePage.CheckedOutToMeGrid.EmptyGridHeaderText,
				EmptyGridHeaderMismatchMessage );
			Assert.AreEqual( "The objects currently checked out to you are shown in this area.",
				homePage.CheckedOutToMeGrid.EmptyGridDescriptionText,
				EmptyGridDescriptionMismatchMessage );
			Assert.AreEqual( 0, homePage.CheckedOutToMeGrid.NumberOfItems,
				ItemCountMismatchMessage );

			// Quit browser because the test uses specific user.
			this.browserManager.EnsureQuitBrowser();
		}

		[Test]
		public void GridMiniViewSelected()
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Make a search.
			homePage.SearchPane.FilteredQuickSearch( "", "Assignment" );

			// Create an assignment and assign it to current user.
			string assignmentName = "GridMiniViewSelected assignment";
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( "Assignment" );
			newMDCard.Properties.SetPropertyValue( "Class", "Assignment", 5 );
			newMDCard.Properties.SetPropertyValue( "Assigned to", this.username );
			newMDCard.Properties.SetPropertyValue( "Name or title", assignmentName );

			// Don't check in immediately.
			newMDCard.CheckInImmediatelyClick();
			newMDCard.SaveAndDiscardOperations.Save();

			// Go back to home view.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Assert that only the specific grid is active/selected.
			Assert.True( homePage.BrowseGrid.GridSelected, GridSelectedErrorMessage );
			Assert.False( homePage.RecentlyAccessedByMeGrid.GridSelected, GridNotSelectedErrorMessage );
			Assert.False( homePage.AssignedToMeGrid.GridSelected, GridNotSelectedErrorMessage );
			Assert.False( homePage.CheckedOutToMeGrid.GridSelected, GridNotSelectedErrorMessage );

			// Select the object in Recently accessed by me grid.
			homePage.RecentlyAccessedByMeGrid.SelectObject( assignmentName );

			// Assert that only the specific grid is active/selected.
			Assert.False( homePage.BrowseGrid.GridSelected, GridNotSelectedErrorMessage );
			Assert.True( homePage.RecentlyAccessedByMeGrid.GridSelected, GridSelectedErrorMessage );
			Assert.False( homePage.AssignedToMeGrid.GridSelected, GridNotSelectedErrorMessage );
			Assert.False( homePage.CheckedOutToMeGrid.GridSelected, GridNotSelectedErrorMessage );

			// Select the object in Recently accessed by me grid.
			homePage.AssignedToMeGrid.SelectObject( assignmentName );

			// Assert that only the specific grid is active/selected.
			Assert.False( homePage.BrowseGrid.GridSelected, GridNotSelectedErrorMessage );
			Assert.False( homePage.RecentlyAccessedByMeGrid.GridSelected, GridNotSelectedErrorMessage );
			Assert.True( homePage.AssignedToMeGrid.GridSelected, GridSelectedErrorMessage );
			Assert.False( homePage.CheckedOutToMeGrid.GridSelected, GridNotSelectedErrorMessage );

			// Select the object in Recently accessed by me grid.
			homePage.CheckedOutToMeGrid.SelectObject( assignmentName );

			// Assert that only the specific grid is active/selected.
			Assert.False( homePage.BrowseGrid.GridSelected, GridNotSelectedErrorMessage );
			Assert.False( homePage.RecentlyAccessedByMeGrid.GridSelected, GridNotSelectedErrorMessage );
			Assert.False( homePage.AssignedToMeGrid.GridSelected, GridNotSelectedErrorMessage );
			Assert.True( homePage.CheckedOutToMeGrid.GridSelected, GridSelectedErrorMessage );
		}

		[Test]
		public void CompletingRemovesAssignmentFromAssignedToMeGridMiniView()
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Define 2 assignments.
			List<string> assignmentNames = new List<string> { "CompletingRemoves assignment1", "CompletingRemoves assignment2" };
			string assignment1 = assignmentNames[ 0 ];
			string assignment2 = assignmentNames[ 1 ];

			// Go through the assignments.
			foreach( string assignmentName in assignmentNames )
			{
				// Assign assignment to current user.
				MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( "Assignment" );
				newMDCard.Properties.SetPropertyValue( "Class", "Assignment", 5 );
				newMDCard.Properties.SetPropertyValue( "Assigned to", this.username );
				newMDCard.Properties.SetPropertyValue( "Name or title", assignmentName );
				newMDCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );

				// Wait until the created assignment appears to the grid.
				homePage.AssignedToMeGrid.WaitUntilItemIsInListing( assignmentName );
			}

			// Assert that both assignments are in grid.
			Assert.True( homePage.AssignedToMeGrid.IsItemInListing( assignment1 ),
				this.FormatItemNotFoundAssertionMessage( assignment1 ) );
			Assert.True( homePage.AssignedToMeGrid.IsItemInListing( assignment2 ),
				this.FormatItemNotFoundAssertionMessage( assignment2 ) );

			// Complete the first assignment.
			MetadataCardRightPane mdCard = homePage.AssignedToMeGrid.SelectObject( assignment1 );
			mdCard.AssignmentOperations.MarkComplete( true, this.username );
			mdCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );

			// Assert that the first assignment is no longer in the grid but the other
			// still is.
			Assert.False( homePage.AssignedToMeGrid.IsItemInListing( assignment1 ),
				this.FormatItemFoundAssertionMessage( assignment1 ) );
			Assert.True( homePage.AssignedToMeGrid.IsItemInListing( assignment2 ),
				this.FormatItemNotFoundAssertionMessage( assignment2 ) );

			// Expand/navigate to the Assigned to me grid view.
			ListView listing = homePage.AssignedToMeGrid.ExpandGrid();

			// Select the assignment and mark it complete.
			mdCard = listing.SelectObject( assignment2 );
			mdCard.AssignmentOperations.MarkComplete( true, this.username );
			mdCard.SaveAndDiscardOperations.Save( metadataCardLoadExpected: false );

			// Go back to home view.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Assert that the assignments are not in the assigned to me grid.
			Assert.False( homePage.AssignedToMeGrid.IsItemInListing( assignment1 ),
				this.FormatItemFoundAssertionMessage( assignment1 ) );
			Assert.False( homePage.AssignedToMeGrid.IsItemInListing( assignment2 ),
				this.FormatItemFoundAssertionMessage( assignment2 ) );
		}

		[Test]
		[TestCase(
			"Sales Invoice 313 - CBH International.xls", "Document",
			"Office Design", "Project" )]
		public void CheckInRemovesObjectFromCheckedOutToMeGridMiniView(
			string objectName1, string objectType1,
			string objectName2, string objectType2 )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for first object and check it out.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName1, objectType1 );
			listing.RightClickItemOpenContextMenu( objectName1 ).CheckOutObject();

			// Go back to home.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Search for another object and check it out.
			listing = homePage.SearchPane.FilteredQuickSearch( objectName2, objectType2 );
			listing.RightClickItemOpenContextMenu( objectName2 ).CheckOutObject();

			// Go back to home.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Assert that both objects are visible in Checked out to me grid.
			Assert.True( homePage.CheckedOutToMeGrid.IsItemInListing( objectName1 ),
				this.FormatItemNotFoundAssertionMessage( objectName1 ) );
			Assert.True( homePage.CheckedOutToMeGrid.IsItemInListing( objectName2 ),
				this.FormatItemNotFoundAssertionMessage( objectName2 ) );

			// Assert that both objects are displayed as checked out in the grid.
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser,
				homePage.CheckedOutToMeGrid.GetObjectCheckOutStatus( objectName1 ),
				$"Mismatch between expected and actual checkout status of object '{objectName1}'." );
			Assert.AreEqual( CheckOutStatus.CheckedOutToCurrentUser,
				homePage.CheckedOutToMeGrid.GetObjectCheckOutStatus( objectName2 ),
				$"Mismatch between expected and actual checkout status of object '{objectName2}'." );

			// Check in the first object.
			homePage.CheckedOutToMeGrid.RightClickItemOpenContextMenu( objectName1 ).CheckInObjectClearsSelection();

			// Assert that the first object is no longer in the grid but the other one is.
			Assert.False( homePage.CheckedOutToMeGrid.IsItemInListing( objectName1 ),
				this.FormatItemFoundAssertionMessage( objectName1 ) );
			Assert.True( homePage.CheckedOutToMeGrid.IsItemInListing( objectName2 ),
				this.FormatItemNotFoundAssertionMessage( objectName2 ) );

			// Expand/navigate to the grid view.
			listing = homePage.CheckedOutToMeGrid.ExpandGrid();

			// Check in the other object.
			listing.RightClickItemOpenContextMenu( objectName2 ).CheckInObjectClearsSelection();

			// Go back to home.
			homePage.TopPane.TabButtons.HomeTabClick();

			// Assert that both objects are no longer in the Checked out to me grid.
			Assert.False( homePage.CheckedOutToMeGrid.IsItemInListing( objectName1 ),
				this.FormatItemFoundAssertionMessage( objectName1 ) );
			Assert.False( homePage.CheckedOutToMeGrid.IsItemInListing( objectName2 ),
				this.FormatItemFoundAssertionMessage( objectName2 ) );


		}
	}
}
