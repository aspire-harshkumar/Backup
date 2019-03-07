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
	class DataTypesColumnValuesInSearch
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


		public DataTypesColumnValuesInSearch()
		{
			this.classID = "DataTypesColumnValuesInSearch";
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
			"Text properties special characters.xlsx",
			"Document",
			"Keywords",
			"Keywords ÅÄÖåäö !\"#¤%&/()=?`*^_:;<>",
			"modified keywords",
			Description = "Text." )]
		[TestCase(
			"Text properties special characters.xlsx",
			"Document",
			"Description",
			"Description ÅÄÖåäö !\"#¤%&/()=?`*^_:;<>",
			"modified description",
			Description = "Multi-line text." )]
		[TestCase(
			"Department SSLU special characters.txt",
			"Document",
			"Department",
			"Department ÅÄÖåäö !\"#¤%&/()=?`*^_:;<>",
			"Sales",
			Description = "Value list SSLU." )]
		[TestCase(
			"Country special characters",
			"Customer",
			"Country",
			"Country ÅÄÖåäö !\\\"#¤%&/()=?`*^_:;<>",
			"Canada",
			Description = "Value list MSLU." )]
		[TestCase(
			"Object type SSLU special characters.pptx",
			"Document",
			"Customer SSLU",
			"Customer ÅÄÖåäö !\"#¤%&/()=?`*^_:;<>",
			"CBH International",
			Description = "Object type SSLU." )]
		[TestCase(
			"Object type MSLU special characters",
			"Project",
			"Customer",
			"Customer ÅÄÖåäö !\"#¤%&/()=?`*^_:;<>",
			"DAT Sports & Entertainment",
			Description = "Object type MSLU." )]
		[TestCase(
			"Project Plan / Records Management.doc",
			"Document",
			"Document date",
			"7/9/2007",
			"12/14/2018",
			Description = "Date." )]
		[TestCase(
			"EditSinglePropertyInOneObject integer.bmp",
			"Document",
			"Integer property",
			"219000",
			"-60985",
			Description = "Integer number." )]
		[TestCase(
			"EditPropertiesOfAllTypesInOneObject.docx",
			"Document",
			"Real number property",
			"195.72",
			"-2964.93",
			Description = "Real number." )]
		[TestCase(
			"Proposal 7701 - City of Chicago (Planning and Development).doc",
			"Document",
			"Accepted",
			"No",
			"Yes",
			Description = "Boolean with value No." )]
		[TestCase(
			"Proposal 7728 - Reece, Murphy and Partners.doc",
			"Document",
			"Accepted",
			"Yes",
			"No",
			Description = "Boolean with value Yes." )]
		[TestCase(
			"EditSinglePropertyInOneObject time.txt",
			"Document",
			"Time property",
			"2:58 PM",
			"7:06 AM",
			Description = "Time." )]
		public void ModifyColumnValueWhenInSearch(
			string objectName,
			string objectType,
			string property,
			string expectedPropertyValue,
			string modifiedValue )
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

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );
			mdCard.Properties.SetPropertyValue( property, modifiedValue );
			mdCard.SaveAndDiscardOperations.Save();

			// Get the value in the specified column.
			string actualModifiedColumnValue = listing.Columns.GetColumnValueOfObject( objectName, property );

			Assert.AreEqual( modifiedValue, actualModifiedColumnValue,
				FormatColumnMismatchMessage( property ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( property );
		}

		[Test]
		[TestCase(
			"Project Meeting Minutes 2/2006.txt",
			"Document",
			"Keywords",
			"",
			Description = "Text empty value." )]
		[TestCase(
			"View from the Sea.jpg",
			"Document",
			"Description",
			"",
			Description = "Multi-line text empty value." )]
		[TestCase(
			"EditPropertiesOfAllTypesInOneObject empty values.docx",
			"Document",
			"Department",
			"",
			Description = "Value list SSLU empty value." )]
		[TestCase(
			"EditSinglePropertyInOneObject textprop",
			"Customer",
			"Country",
			"",
			Description = "Value list MSLU empty value." )]
		[TestCase(
			"EditPropertiesOfAllTypesInOneObject empty values.docx",
			"Document",
			"Owner",
			"",
			Description = "Object type SSLU empty value." )]
		[TestCase(
			"Lake.jpg",
			"Document",
			"Customer",
			"",
			Description = "Object type MSLU empty value." )]
		[TestCase(
			"Project Schedule (Sales Strategy Development).pdf",
			"Document",
			"Document date",
			"",
			Description = "Date empty value." )]
		[TestCase(
			"EditPropertiesOfAllTypesInOneObject empty values.docx",
			"Document",
			"Time property",
			"",
			Description = "Time empty value." )]
		[TestCase(
			"EditPropertiesOfAllTypesInOneObject empty values.docx",
			"Document",
			"Integer property",
			"",
			Description = "Integer number empty value." )]
		[TestCase(
			"EditPropertiesOfAllTypesInOneObject empty values.docx",
			"Document",
			"Real number property",
			"",
			Description = "Real number empty value." )]
		[TestCase(
			"Proposal 7720 - ESTT Corporation (IT).doc",
			"Document",
			"Accepted",
			"",
			Description = "Boolean empty value." )]
		[TestCase(
			"Project Meeting Minutes 2/2006.txt",
			"Document",
			"City",
			"-",
			Description = "Text property missing." )]
		[TestCase(
			"View from the Sea.jpg",
			"Document",
			"Assignment description",
			"-",
			Description = "Multi-line text property missing." )]
		[TestCase(
			"Staff Training / ERP",
			"Project",
			"Department",
			"-",
			Description = "Value list SSLU property missing." )]
		[TestCase(
			"Power Line Test Results.doc",
			"Document",
			"Country",
			"-",
			Description = "Value list MSLU property missing." )]
		[TestCase(
			"Sales Invoice 313 - CBH International.xls",
			"Document",
			"Owner",
			"-",
			Description = "Object type SSLU property missing." )]
		[TestCase(
			"Project Plan / Feasibility Study.doc",
			"Document",
			"Customer",
			"-",
			Description = "Object type MSLU property missing." )]
		[TestCase(
			"Project Schedule (Sales Strategy Development).pdf",
			"Document",
			"Deadline",
			"-",
			Description = "Date property missing." )]
		[TestCase(
			"Project Meeting Minutes 1/2006.txt",
			"Document",
			"Time property",
			"-",
			Description = "Time property missing." )]
		[TestCase(
			"Arch.jpg",
			"Document",
			"Integer property",
			"-",
			Description = "Integer number property missing." )]
		[TestCase(
			"Annual General Meeting Agenda.doc",
			"Document",
			"Real number property",
			"-",
			Description = "Real number property missing." )]
		[TestCase(
			"Order for Electrical Engineering.doc",
			"Document",
			"Accepted",
			"-",
			Description = "Boolean property missing." )]
		public void EmptyColumnValueWhenInsertColumnInSearch(
			string objectName,
			string objectType,
			string columnName,
			string expectedValue )
		{

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Search for object.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			// Insert column to search results view.
			listing.Columns.InsertColumnByContextMenu( columnName );

			// Get the value in the specified column.
			string columnValue = listing.Columns.GetColumnValueOfObject( objectName, columnName );

			// Assert that the value displayed in column is empty.
			Assert.AreEqual( expectedValue, columnValue, FormatColumnMismatchMessage( columnName ) );

			// Remove the column.
			listing.Columns.RemoveColumnByContextMenu( columnName );
		}
	}
}
