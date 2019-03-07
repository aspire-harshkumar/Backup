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
	[Order( -7 )]
	[Parallelizable( ParallelScope.Self )]
	class ColumnValuesInSearch
	{
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


		public ColumnValuesInSearch()
		{
			this.classID = "ColumnValuesInSearch";
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
		/// Get informative error message for mismatch between expected and actual 
		/// column value.
		/// </summary>
		/// <param name="columnName">Name of the column printed to error message.</param>
		private string FormatColumnMismatchMessage( string columnName )
		{
			return $"Mismatch between expected and actual value in column '{columnName}'.";
		}

		[Test]
		[TestCase(
			"Project Announcement / Austin.pdf",
			"Document",
			"Customer",
			"A&A Consulting (AEC);RGPP Partnership",
			"RGPP Partnership",
			"Fortney Nolte Associates",
			"A&A Consulting (AEC);Fortney Nolte Associates",
			Description = "Object type MSLU with 2 values selected." )]
		[TestCase(
			"Two countries",
			"Customer",
			"Country",
			"Germany;France",
			"Germany",
			"China",
			"China;France",
			Description = "Value list MSLU with 2 values selected." )]
		public void MsluPropertyColumnWhenInSearch(
			string objectName,
			string objectType,
			string property,
			string expectedColumnValue,
			string replaceThisValue,
			string modifiedValue,
			string expectedColumnValueAfterModification )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( property );

			// Get the value in the specified column.
			string actualColumnValue = listing.Columns.GetColumnValueOfObject( objectName, property );

			// Assert that the value is displayed as expected.
			Assert.AreEqual( expectedColumnValue, actualColumnValue,
				FormatColumnMismatchMessage( property ) );

			// Modify the MSLU property by replacing a value with another value.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
			mdCard.Properties.SetMultiSelectLookupPropertyValue( property, modifiedValue, replaceThisValue );
			mdCard.SaveAndDiscardOperations.Save();

			// Get the value in the specified column.
			string actualModifiedColumnValue = listing.Columns.GetColumnValueOfObject( objectName, property );

			// Assert that the value is displayed as expected.
			Assert.AreEqual( expectedColumnValueAfterModification, actualModifiedColumnValue,
				FormatColumnMismatchMessage( property ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( property );
		}

		[Test]
		[TestCase(
			"Workflow and state special characters.docx",
			"Document",
			"WF ÅÄÖåäö !\"#¤%&/()=?`*^_:;<>",
			"Processing job applications",
			Description = "Built-in property Workflow." )]
		public void WorkflowColumnWhenInSearch(
			string objectName,
			string objectType,
			string expectedWorkflow,
			string modifiedWorkflow )
		{
			string workflowColumnName = "Workflow";

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( workflowColumnName );

			// Get the value in the specified column.
			string actualColumnValue = listing.Columns.GetColumnValueOfObject( objectName, workflowColumnName );

			// Assert that the value is displayed as expected.
			Assert.AreEqual( expectedWorkflow, actualColumnValue,
				FormatColumnMismatchMessage( workflowColumnName ) );

			// Set the state as no state.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
			mdCard.Workflows.SetWorkflowStateTransition( "(no state)" );
			mdCard.SaveAndDiscardOperations.Save();

			// Set new workflow.
			mdCard.Workflows.SetWorkflow( modifiedWorkflow );
			mdCard.SaveAndDiscardOperations.Save();

			// Get the value in the specified column.
			string actualModifiedColumnValue = listing.Columns.GetColumnValueOfObject( objectName, workflowColumnName );

			Assert.AreEqual( modifiedWorkflow, actualModifiedColumnValue,
				FormatColumnMismatchMessage( workflowColumnName ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( workflowColumnName );
		}

		[TestCase(
			"Invoice AB887-23-22A.pdf",
			"Document",
			"Received, awaiting checking",
			"Checked, awaiting approval",
			Description = "Built-in property Workflow State." )]
		[Test]
		public void WorkflowStateColumnWhenInSearch(
			string objectName,
			string objectType,
			string expectedWorkflowState,
			string modifiedWorkflowState )
		{
			string workflowStateColumnName = "Workflow State";

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( workflowStateColumnName );

			// Get the value in the specified column.
			string actualColumnValue = listing.Columns.GetColumnValueOfObject( objectName, workflowStateColumnName );

			// Assert that the value is displayed as expected.
			Assert.AreEqual( expectedWorkflowState, actualColumnValue,
				FormatColumnMismatchMessage( workflowStateColumnName ) );

			// Set the state as no state.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
			mdCard.Workflows.SetWorkflowStateTransition( modifiedWorkflowState );
			mdCard.SaveAndDiscardOperations.Save();

			// Get the value in the specified column.
			string actualModifiedColumnValue = listing.Columns.GetColumnValueOfObject( objectName, workflowStateColumnName );

			// Assert that the modified workflow state is displayed in the column.
			Assert.AreEqual( modifiedWorkflowState, actualModifiedColumnValue,
				FormatColumnMismatchMessage( workflowStateColumnName ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( workflowStateColumnName );
		}


		[Test]
		[TestCase(
			"Comment special characters.docx",
			"Document",
			"Comment ÅÄÖåäö !\\\"#¤%&/()=?`*^_:;<>",
			"New comment",
			Description = "Built-in property Comment." )]
		public void CommentColumnWhenInSearch(
			string objectName,
			string objectType,
			string expectedComment,
			string addedComment )
		{

			string commentColumnName = "Comment";

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( commentColumnName );

			// Get the value in the specified column.
			string actualCommentColumnValue = listing.Columns.GetColumnValueOfObject( objectName, commentColumnName );

			// Assert that the value is displayed as expected.
			Assert.AreEqual( expectedComment, actualCommentColumnValue,
				FormatColumnMismatchMessage(commentColumnName) );

			// Set new comment.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
			mdCard.Header.GoToComments();
			mdCard.Comments.SetComments( addedComment );
			mdCard.SaveAndDiscardOperations.Save();

			// Get the value in the specified column.
			string actualModifiedCommentColumnValue = listing.Columns.GetColumnValueOfObject( objectName, commentColumnName );

			Assert.AreEqual( addedComment, actualModifiedCommentColumnValue,
				FormatColumnMismatchMessage( commentColumnName ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( commentColumnName );
		}

		[Test]
		[TestCase(
			"Company Brochure / ESTT.doc",
			"Document",
			"ID",
			"123",
			Description = "Built-in property ID." )]
		[TestCase(
			"Door Chart A1.dwg",
			"Document",
			"Date Created",
			"11/25/2004 9:39 PM",
			Description = "Built-in property Date Created." )]
		[TestCase(
			"Power Line Specs.doc",
			"Document",
			"Object Type",
			"Document",
			Description = "Built-in property Object Type for Document." )]
		[TestCase(
			"Office Design",
			"Project",
			"Object Type",
			"Project",
			Description = "Built-in property Object Type for Non-document." )]
		public void BuiltInPropertyColumnValuesInSearch(
			string objectName,
			string objectType,
			string property,
			string expectedPropertyValue )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( property );

			// Get the value in the specified column.
			string actualColumnValue = listing.Columns.GetColumnValueOfObject( objectName, property );

			// Assert that the value is displayed as expected.
			Assert.AreEqual( expectedPropertyValue, actualColumnValue,
				FormatColumnMismatchMessage( property ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( property );
		}

		[Test]
		[TestCase(
			"Sales Invoice 240 - A&A Consulting (AEC).xls",
			"Document",
			"Keywords",
			"update",
			Description = "Built-in property Version." )]
		public void VersionColumnInSearch(
			string objectName,
			string objectType,
			string property,
			string value )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			string versionColumn = "Version";

			// Search for object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( versionColumn );

			// Check object's current version.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
			int expectedVersion = mdCard.Header.Version;

			// Get the value in the Version column.
			string actualColumnValue = listing.Columns.GetColumnValueOfObject( objectName, versionColumn );

			// Assert that the value is displayed as expected.
			Assert.AreEqual( expectedVersion.ToString(), actualColumnValue,
				FormatColumnMismatchMessage( versionColumn ) );

			// Modify metadata to increase object's version.
			mdCard.Properties.SetPropertyValue( property, value );
			mdCard.SaveAndDiscardOperations.Save();
			int newExpectedVersion = expectedVersion + 1;

			// Get the value in the Version column.
			string actualNewColumnValue = listing.Columns.GetColumnValueOfObject( objectName, versionColumn );

			// Assert that the value is displayed as expected.
			Assert.AreEqual( newExpectedVersion.ToString(), actualNewColumnValue,
				FormatColumnMismatchMessage( versionColumn ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( versionColumn );
		}

		[Test]
		[TestCase(
			"Logo Design / RMP",
			"Project",
			"Full control for all internal users",
			"Only for me",
			Description = "Built-in property Permissions." )]
		public void PermissionsColumnInSearch(
			string objectName,
			string objectType,
			string expectedPermissions,
			string newPermissions )
		{

			string permissionsColumnName = "Permissions";

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( permissionsColumnName );

			// Get the value in the specified column.
			string columnValue = listing.Columns.GetColumnValueOfObject( objectName, permissionsColumnName );

			// Assert the value displayed in Permissions column.
			Assert.AreEqual( expectedPermissions, columnValue,
				FormatColumnMismatchMessage( permissionsColumnName ) );

			// Change permissions and save.
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
			mdCard.Permissions.SetPermission( newPermissions );
			mdCard.SaveAndDiscardOperations.Save();

			// Get the value in the specified column.
			string newColumnValue = listing.Columns.GetColumnValueOfObject( objectName, permissionsColumnName );

			// Assert the value displayed in Permissions column.
			Assert.AreEqual( newPermissions, newColumnValue,
				FormatColumnMismatchMessage( permissionsColumnName ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( permissionsColumnName );
		}

		[Test]
		[TestCase(
			"Customer with hidden country",
			"Customer",
			"Country",
			Description = "Hidden value list MSLU value." )]
		[TestCase(
			"Doc with hidden customer.txt",
			"Document",
			"Customer",
			Description = "Hidden object type MSLU value." )]
		public void HiddenColumnValueWhenInsertColumnInSearch(
			string objectName,
			string objectType,
			string columnName )
		{

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( columnName );

			// Get the value in the specified column.
			string columnValue = listing.Columns.GetColumnValueOfObject( objectName, columnName );

			// Assert that the value is displayed as (hidden) in column.
			Assert.AreEqual( "(hidden)", columnValue, FormatColumnMismatchMessage( columnName ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( columnName );
		}

		[Test]
		[TestCase(
			"Proposal 7707 - Lance Smith Engineering (Surveying).doc",
			"Document",
			"Project",
			"Initial Land Survey / Central Plains",
			"Central Plains Area Development" )]
		public void CheckoutObjectColumnValueInSearch(
			string objectName,
			string objectType,
			string columnName,
			string initialColumnValue,
			string modifyColumnValue )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( columnName );

			// Assert that the value is displayed in column before checkout.
			string columnValueBeforeCheckout = listing.Columns.GetColumnValueOfObject( objectName, columnName );
			Assert.AreEqual( initialColumnValue, columnValueBeforeCheckout,
				FormatColumnMismatchMessage( columnName ) );

			MetadataCardRightPane mdCard = listing.RightClickItemOpenContextMenu( objectName ).CheckOutObject();
			
			// Assert that the value is displayed in column after checkout.
			string columnValueAfterCheckout = listing.Columns.GetColumnValueOfObject( objectName, columnName );
			Assert.AreEqual( initialColumnValue, columnValueAfterCheckout,
				FormatColumnMismatchMessage( columnName ) );

			mdCard.Properties.SetPropertyValue( columnName, modifyColumnValue );
			mdCard.SaveAndDiscardOperations.Save();
			
			// Assert that the value is displayed in column after edit.
			string columnValueAfterModify = listing.Columns.GetColumnValueOfObject( objectName, columnName );
			Assert.AreEqual( modifyColumnValue, columnValueAfterModify,
				FormatColumnMismatchMessage( columnName ) );

			listing.RightClickItemOpenContextMenu( objectName ).CheckInObject();

			// Assert that the value is displayed in column after check in.
			string columnValueAfterCheckIn = listing.Columns.GetColumnValueOfObject( objectName, columnName );
			Assert.AreEqual( modifyColumnValue, columnValueAfterCheckIn,
				FormatColumnMismatchMessage( columnName ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( columnName );
		}

		[Test]
		[TestCase(
			"development",
			"Project",
			"Column project",
			"Name or title",
			"Customer Project",
			6,
			"Customer;In progress",
			"DAT Sports & Entertainment;Yes" )]
		public void CreateNewObjectAndVerifyColumnValuesInSearch(
			string searchWord,
			string objectType,
			string objectName,
			string nameProperty,
			string classValue,
			int numberOfPropsInClass,
			string columnNamesString,
			string columnValuesString )
		{
			// Right click this column to add more columns to its left side.
			string rightClickColumn = "Date Modified";

			List<string> columnNames = columnNamesString.Split( ';' ).ToList();
			List<string> columnValues = columnValuesString.Split( ';' ).ToList();

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for objects.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( searchWord, objectType );

			// Insert some built-in property columns.
			List<string> builtInProperties = new List<string> { "Class", "Object Type", "Version", "ID" };
			foreach( string prop in builtInProperties )
			{
				listing.Columns.InsertColumnByContextMenu( prop, rightClickColumn );
			}

			// Insert more columns.
			foreach( string columnName in columnNames )
			{
				// Insert column to search results view.
				listing.Columns.InsertColumnByContextMenu( columnName, rightClickColumn );
			}

			// Create new object.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObject( objectType );
			newMDCard.Properties.SetPropertyValue( "Class", classValue, numberOfPropsInClass );
			newMDCard.Properties.SetPropertyValue( nameProperty, objectName );

			// Set values to properties whose columns were inserted.
			for( int i = 0; i < columnNames.Count; ++i )
			{
				newMDCard.Properties.SetPropertyValue( columnNames[ i ], columnValues[ i ] );
			}

			MetadataCardRightPane mdCard = newMDCard.SaveAndDiscardOperations.Save();

			// Get object ID.
			string objectID = mdCard.Header.ID;

			// Assert built-in property values in columns.
			Assert.AreEqual( classValue, listing.Columns.GetColumnValueOfObject( objectName, "Class" ),
				FormatColumnMismatchMessage( "Class") );
			Assert.AreEqual( objectType, listing.Columns.GetColumnValueOfObject( objectName, "Object Type" ),
				FormatColumnMismatchMessage( "Object Type" ) );
			Assert.AreEqual( "1", listing.Columns.GetColumnValueOfObject( objectName, "Version" ),
				FormatColumnMismatchMessage( "Version" ) );
			Assert.AreEqual( objectID, listing.Columns.GetColumnValueOfObject( objectName, "ID" ),
				FormatColumnMismatchMessage( "ID" ) );

			// Assert the rest of the column values.
			for( int i = 0; i < columnNames.Count; ++i )
			{
				Assert.AreEqual( columnValues[ i ], listing.Columns.GetColumnValueOfObject( objectName, columnNames[ i ] ),
					FormatColumnMismatchMessage( columnNames[ i ] ) );
			}

			// Remove the built-in property columns.
			foreach( string prop in builtInProperties )
			{
				listing.Columns.RemoveColumnByContextMenu( prop );
			}

			// Remove the rest of the inserted columns.
			foreach( string columnName in columnNames )
			{
				listing.Columns.RemoveColumnByContextMenu( columnName );
			}
		}
	}
}
