using System.Collections.Generic;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -8 )]
	[Category( "ExternalRepository" )]
	[Parallelizable( ParallelScope.Self )]
	class ExternalRepositorySearch : FiltersInSearch
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected new readonly string classID;

		public ExternalRepositorySearch()
		{
			this.classID = "ExternalRepositorySearch";
		}

		[OneTimeSetUp]
		public override void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "NFC_sample_vault.mfb", users );

			// Assign the vault name in local variable.
			this.vaultName = this.mfContext.VaultName;

			// TODO: The "user" identifier here is now defined in SetupHelper. Maybe this should come from configuration and
			// it should also be given to the SetupHelper as parameter.
			this.username = this.mfContext.UsernameOfUser( "user" );
			this.password = this.mfContext.PasswordOfUser( "user" );

			this.browserManager = new TestClassBrowserManager( this.configuration, this.username, this.password, this.vaultName );

			// Configure the Network Folder Connector in the vault.
			EnvironmentSetupHelper.ConfigureNetworkFolderConnectorToVault( this.mfContext, this.classID );

			// Promote objects in the vault.
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "sample_pdfa.pdf" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "article.aspx.txt" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "MultipleRecipientsInTOCC.msg" );
			EnvironmentSetupHelper.PromoteObject( this.mfContext, "SubFolder3\\Asennusohje - M-Files laiterekisteri.docx" );
		}

		[OneTimeTearDown]
		public void ExternalRepositoryCleanup()
		{
			EnvironmentSetupHelper.ClearExternalRepository( this.classID );
		}

		/// <summary>
		/// Search external object with document object type as search filter.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"Document",
			"Documents",
			"Income Statement 10 2006-6.xlsm",
			Description = "Unmanaged object." )]
		[TestCase(
			"Document",
			"Documents",
			"Big_List_of_Naughty_Strings.txt",
			Description = "Unmanaged object." )]
		[TestCase(
			"Document",
			"Documents",
			"article.aspx.txt",
			Description = "Managed object." )]
		[TestCase(
			"Document",
			"Documents",
			"Asennusohje - M-Files laiterekisteri.docx",
			Description = "Managed object." )]
		[TestCase(
			"",
			"Documents",
			"Minutes with M-Files Properties.doc",
			Description = "External object search with location filter." )]
		[TestCase(
			"",
			"Documents",
			"exportdatabase1.vdx",
			Description = "External object search with location filter." )]
		public override void ObjectTypeFilterSearch(
			string objectType,
			string expectedHeaderText,
			string objectName )
		{
			// Check and assign the connection name as objectType for the external repository test cases.
			if( objectType.Equals( "" ) )
				objectType = this.classID;

			// Execute the test by calling the base class method with the external repository data.
			base.ObjectTypeFilterSearch( objectType, expectedHeaderText, objectName );
		}

		/// <summary>
		/// Set two object types as search filter. Using document for external object type and a non-document object types as filter.
		/// Document grouping header are expanded by default.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"RGPP Partnership",
			"Document",
			"Customer",
			"Documents",
			"Customers",
			"RGPP Partnership.txt",
			"RGPP Partnership" )]
		public override void MultipleObjectTypeFiltersSearchDocumentAndNonDocument(
			string searchWord,
			string filter1,
			string filter2,
			string header1,
			string header2,
			string object1,
			string object2 )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.MultipleObjectTypeFiltersSearchDocumentAndNonDocument( searchWord, filter1, filter2, header1, header2, object1, object2 );
		}

		/// <summary>
		/// Set two object types as search filter. Using two a non-document object types as filter.
		/// Non-document object type grouping headers are collapsed by default.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"Bill Richards",
			"Employee",
			"Project",
			"Employees",
			"Projects",
			"Bill Richards",
			"Office Design" )]
		public override void MultipleObjectTypeFiltersSearchTwoNonDocumentTypes(
			string searchWord,
			string filter1,
			string filter2,
			string header1,
			string header2,
			string object1,
			string object2 )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.MultipleObjectTypeFiltersSearchTwoNonDocumentTypes( searchWord, filter1, filter2, header1, header2, object1, object2 );
		}

		/// <summary>
		/// First make a search without filters. Then apply one object type filter and verify that the results are filtered.
		/// Then add another object type as search filter and verify that the search results update again to match the filter.
		/// Using document/document collection and a non-document object types as filter. Document and document collection grouping
		/// headers are expanded by default.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"Bill Richards",
			"Document",
			"Employee",
			"Documents",
			"Employees",
			"Bill Richards.docx",
			"Bill Richards" )]
		public override void FilterSearchResultsByObjectTypeDocumentAndNonDocument(
			string searchword,
			string filter1,
			string filter2,
			string header1,
			string header2,
			string object1,
			string object2 )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.FilterSearchResultsByObjectTypeDocumentAndNonDocument( searchword, filter1, filter2, header1, header2, object1, object2 );
		}

		/// <summary>
		/// First make a search without filters. Then apply one object type filter and verify that the results are filtered.
		/// Then add another object type as search filter and verify that the search results update again to match the filter.
		/// Using two non-document object types as filters. Non-document grouping headers are collapsed by default.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"RGPP Partnership",
			"Contact person",
			"Customer",
			"Contact persons",
			"Customers",
			"Walter Johnson",
			"RGPP Partnership" )]
		public override void FilterSearchResultsByObjectTypeTwoNonDocumentTypes(
			string searchword,
			string filter1,
			string filter2,
			string header1,
			string header2,
			string object1,
			string object2 )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.FilterSearchResultsByObjectTypeTwoNonDocumentTypes( searchword, filter1, filter2, header1, header2, object1, object2 );
		}

		/// <summary>
		/// Search by using the filter to look only in metadata. Check that an expected external object is found but also that
		/// an internal object is not found that has the search word in its file contents but not in metadata.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"Vivamus pretium",
			"Hahmotelmaa M-Files HR -järjestelmän - Vivamus pretium.mht",
			"Request for Proposal - Graphical Design.doc" )]
		public override void MetadataFilterSearch(
			string searchWord,
			string expectedObj,
			string notExpectedObj )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.MetadataFilterSearch( searchWord, expectedObj, notExpectedObj );
		}

		/// <summary>
		/// Search by using the filter to look only in file contents. Check that an expected external object is found but also that
		/// an external object is not found that has the search word in its metadata but not in file contents.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"chicago",
			"Minutes Project Meeting 4 2007-9.rtf",
			"exportdatabase3 - chicago.vss" )]
		public override void FileContentsFilterSearch(
			string searchword,
			string expectedObj,
			string notExpectedObj )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.FileContentsFilterSearch( searchword, expectedObj, notExpectedObj );
		}

		/// <summary>
		/// Search by using the filter to look in both file contents and metadata card of both internal and external objects. 
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"Robert Brown",
			"Documents;Contact persons;Projects",
			"article.aspx.txt;Minutes Project Meeting 4 2007-3.odt;Robert Brown;CRM Application Development" )]
		public override void MetadataAndFileContentsFilterSearch(
			string searchword,
			string expectedGrpHeaders,
			string expectedObjs )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.MetadataAndFileContentsFilterSearch( searchword, expectedGrpHeaders, expectedObjs );
		}

		/// <summary>
		/// First make a search without filter. Then apply filter to look only in metadata. Check that an expected external object 
		/// is found but also that an external object is not found that has the search word in its file contents but not in metadata.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"Vivamus pretium",
			"Hahmotelmaa M-Files HR -järjestelmän - Vivamus pretium.mht",
			"Request for Proposal - Graphical Design.doc" )]
		public override void FilterSearchResultsByMetadata(
			string searchword,
			string expectedObj,
			string notExpectedObjAfterFilter )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.FilterSearchResultsByMetadata( searchword, expectedObj, notExpectedObjAfterFilter );
		}

		/// <summary>
		/// First make a search without filter. Then apply filter to look only in file contents. Check that an expected external object 
		/// is found but also that an external object is not found that has the search word in its metadata but not in file contents.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"chicago",
			"Minutes Project Meeting 4 2007-9.rtf",
			"exportdatabase3 - chicago.vss" )]
		public override void FilterSearchResultsByFileContents(
			string searchword,
			string expectedObj,
			string notExpectedObjAfterFilter )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.FilterSearchResultsByFileContents( searchword, expectedObj, notExpectedObjAfterFilter );
		}

		/// <summary>
		/// Make a search and verify that all expected search filters and Locations are available to refine the search.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase( "Scope:Metadata,File contents;Object type:Document,Assignment,Contact person,Customer,Document collection,Employee,Project" )]
		public override void AvailableFiltersInSearchResults( string expectedFilters )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.AvailableFiltersInSearchResults( expectedFilters + ";Location:" + this.vaultName + "," + this.classID );
		}

		/// <summary>
		/// Go to a external view and then verify that all expected search filters are available to make a normal search.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase( "", "Scope:Metadata,File contents;Object type:Document,Assignment,Contact person,Customer,Document collection,Employee,Project" )]
		public override void AvailableFiltersInView( string connectionName, string expectedFilters )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.AvailableFiltersInView( this.classID, expectedFilters + ";Location:" + this.vaultName + "," + this.classID );
		}

		/// <summary>
		/// Expected search filters should be available when at home view when external view is configured.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase( "Scope:Metadata,File contents;Object type:Document,Assignment,Contact person,Customer,Document collection,Employee,Project" )]
		public override void AvailableFiltersInHome( string expectedFilters )
		{
			// Execute the test by calling the base class method with the external repository data.
			base.AvailableFiltersInHome( expectedFilters + ";Location:" + this.vaultName + "," + this.classID );
		}

		/// <summary>
		/// Expected external/internal object should be listed based on the different search keywords.
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"\"Test Strategy - Equation - 06 / April\"",
			"Word file with M-Files Properties.doc;RGPP Partnership.txt",
			2,
			Description = "Search keyword within quotes [For e.g.: \"Document content\"]." )]
		[TestCase(
			"845~~846",
			"RGPP Partnership.txt;article.aspx.txt",
			2,
			Description = "A numeric range search with '~~' operator." )]
		[TestCase(
			"06/April",
			"Word file with M-Files Properties.doc;RGPP Partnership.txt",
			2,
			Description = "Search with Date." )]
		[TestCase(
			"RelayedThroughDifferentServers.msg",
			"RelayedThroughDifferentServers.msg",
			1,
			Description = "Search with document object name." )]
		[TestCase(
			"Helen Chase",
			"Helen Chase",
			1,
			Description = "Search with non-document object name." )]
		[TestCase(
			"ExternalRepositorySearch Document",
			"Asennusohje - M-Files laiterekisteri.docx;sample_pdfa.pdf;MultipleRecipientsInTOCC.msg;article.aspx.txt",
			4,
			Description = "Search with Multiple/Compound words of managed object." )]
		public void DifferentKeywordSearchWithExternalObjects(
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

		// Overriding the test that are not relevant for external repositories at the moment and Overriding is done without Test attribute so NUnit will not execute them.
		public override void SearchWithinView( string view, string searchWord, string expectedObjectsString )
		{
			return;
		}
	}
}