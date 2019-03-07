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
	[Order( -5 )]
	[Parallelizable( ParallelScope.Self )]
	class ColumnsInsertAndRemove
	{

		private static readonly string ColumnsAssertionMessage = "Mismatch between expected and actual columns.";

		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		private string username;
		private string password;
		private string vaultName;

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;


		public ColumnsInsertAndRemove()
		{
			this.classID = "ColumnsInsertAndRemove";
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
		/// Grouping header in listing stays collapsed when columns are inserted and when
		/// column is sorted.
		/// </summary>
		[Test]
		[TestCase( "Customer;Keywords", "Documents" )]
		public void AddColumnsWhenGroupingHeaderCollapsed(
			string columns,
			string groupingHeader )
		{
			List<string> addColumns = StringSplitHelper.ParseStringToStringList( columns, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Make an empty search.
			ListView listing = homePage.SearchPane.QuickSearch( "" );

			// Collapse grouping header.
			listing.GroupingHeaders.CollapseGroup( groupingHeader );

			// Insert some columns.
			foreach( string column in addColumns )
			{
				listing.Columns.InsertColumnByContextMenu( column );
			}

			// Sort ascending by some column.
			listing.Columns.SortAscendingByContextMenu( addColumns[ 0 ] );

			// Assert that the grouping header is still collapsed.
			Assert.AreEqual(
				GroupingHeadersInListView.GroupStatus.Collapsed,
				listing.GroupingHeaders.GetGroupStatus( groupingHeader ) );
		}

		/// <summary>
		/// Removing name column should be disabled in context menu and clicking the 
		/// disabled menu item will not remove the name column.
		/// </summary>
		[Test]
		[TestCase( "1. Documents>By Class>Drawing" )]
		public void NameColumnCannotBeRemoved( string view )
		{
			// Name column.
			string nameColumn = "Name";

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to a view.
			ListView listing = homePage.ListView.NavigateToView( view );

			// Click the disabled remove column.
			listing.Columns.ClickDisabledContextMenuItem( ColumnsInListView.PrimaryMenuOption.RemoveColumn, nameColumn );

			// Get all columns.
			List<string> actualColumns = listing.Columns.Headers;

			// Assert that Name column was not removed.
			Assert.True( actualColumns.Contains( nameColumn ),
				$"Column '{nameColumn}' was removed even though user should not be able to remove it." );

			// The context menu is still open at this point. Therefore, insert
			// another column and remove it in order to close the original context menu.
			listing.Columns.InsertColumnByContextMenu( "Accepted" );
			listing.Columns.RemoveColumnByContextMenu( "Accepted" );

		}

		/// <summary>
		/// Use quick search and then insert columns and verify that they are added. 
		/// Then remove the columns and verify that they are removed.
		/// </summary>
		[Test]
		[TestCase(
			"Customer;Keywords",
			"Name;Date Modified;Score;Customer;Keywords",
			"Name;Date Modified;Score",
			null,
			Description = "Insert columns to the right side in empty space." )]
		[TestCase(
			"Object Type;Created by",
			"Name;Date Modified;Object Type;Created by;Score",
			"Name;Date Modified;Score",
			"Score",
			Description = "Insert columns between current columns." )]
		public void InsertAndAddColumnsInSearch(
			string addColumns,
			string allColumns,
			string defaultColumns,
			string rightClickColumn )
		{
			List<string> columns = StringSplitHelper.ParseStringToStringList( addColumns, ';' );
			List<string> expectedColumns = StringSplitHelper.ParseStringToStringList( allColumns, ';' );
			List<string> expectedDefaultColumns = StringSplitHelper.ParseStringToStringList( defaultColumns, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Make an empty search.
			ListView listing = homePage.SearchPane.QuickSearch( "" );

			// Insert some columns.
			foreach( string column in columns )
			{
				listing.Columns.InsertColumnByContextMenu( column, rightClickColumn );
			}

			// Assert that the columns are added.
			Assert.AreEqual( expectedColumns, listing.Columns.Headers,
				ColumnsAssertionMessage );

			// Remove the columns.
			foreach( string column in columns )
			{
				listing.Columns.RemoveColumnByContextMenu( column );
			}

			// Assert that the columns are removed.
			Assert.AreEqual( expectedDefaultColumns, listing.Columns.Headers,
				ColumnsAssertionMessage );
		}

		/// <summary>
		/// Go to a view and then insert columns and verify that they are added. 
		/// Then remove the columns and verify that they are removed.
		/// </summary>
		[Test]
		[TestCase(
			"2. Manage Customers",
			"Address (line 1);Country",
			"Name;City;State/province;Telephone number;Address (line 1);Country",
			"Name;City;State/province;Telephone number",
			null,
			Description = "Normal view. Insert columns to right side empty space." )]
		[TestCase(
			"1. Documents>By Class>Order",
			"ID;Document date",
			"Name;Project;Customer;ID;Document date;Size;Date Modified",
			"Name;Project;Customer;Size;Date Modified",
			"Size",
			Description = "Virtual folder. Insert columns between current columns." )]
		public void InsertAndAddColumnsInViews(
			string view,
			string addColumns,
			string allColumns,
			string defaultColumns,
			string rightClickColumn )
		{
			List<string> columns = StringSplitHelper.ParseStringToStringList( addColumns, ';' );
			List<string> expectedColumns = StringSplitHelper.ParseStringToStringList( allColumns, ';' );
			List<string> expectedDefaultColumns = StringSplitHelper.ParseStringToStringList( defaultColumns, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.ListView.NavigateToView( view );

			// Insert some columns.
			foreach( string column in columns )
			{
				listing.Columns.InsertColumnByContextMenu( column, rightClickColumn );
			}

			// Assert that the columns are added.
			Assert.AreEqual( expectedColumns, listing.Columns.Headers,
				ColumnsAssertionMessage );

			// Remove the columns.
			foreach( string column in columns )
			{
				listing.Columns.RemoveColumnByContextMenu( column );
			}

			// Assert that the columns are removed.
			Assert.AreEqual( expectedDefaultColumns, listing.Columns.Headers,
				ColumnsAssertionMessage );
		}

		/// <summary>
		/// Go to a special view listed under "Other views" and then insert columns and verify that they are added. 
		/// Then remove the columns and verify that they are removed.
		/// </summary>
		[Test]
		[TestCase(
			"Checked Out to Me",
			"Class;Project",
			"Name;Size;Date Modified;Class;Project",
			"Name;Size;Date Modified",
			null )]
		[TestCase(
			"Assigned to Me",
			"Assigned to;Class",
			"Name;Deadline;Assigned to;Class;Assignment description",
			"Name;Deadline;Assignment description",
			"Assignment description" )]
		public void InsertAndAddColumnInOtherViews(
			string view,
			string addColumns,
			string allColumns,
			string defaultColumns,
			string rightClickColumn )
		{

			List<string> columns = StringSplitHelper.ParseStringToStringList( addColumns, ';' );
			List<string> expectedColumns = StringSplitHelper.ParseStringToStringList( allColumns, ';' );
			List<string> expectedDefaultColumns = StringSplitHelper.ParseStringToStringList( defaultColumns, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to a view under the other views section.
			homePage.ListView.GroupingHeaders.ExpandGroup( "Other Views" );
			ListView listing = homePage.ListView.NavigateToView( view );

			// Insert some columns.
			foreach( string column in columns )
			{
				listing.Columns.InsertColumnByContextMenu( column, rightClickColumn );
			}

			// Assert that the columns are added.
			Assert.AreEqual( expectedColumns, listing.Columns.Headers,
				ColumnsAssertionMessage );

			// Remove the columns.
			foreach( string column in columns )
			{
				listing.Columns.RemoveColumnByContextMenu( column );
			}

			// Assert that the columns are removed.
			Assert.AreEqual( expectedDefaultColumns, listing.Columns.Headers,
				ColumnsAssertionMessage );

		}
	}
}
