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
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -6 )]
	[Parallelizable( ParallelScope.Self )]
	class FiltersInSearch
	{

		private static string GroupingHeaderMismatchMessage =
			"Mismatch between expected and actual grouping headers in listing.";

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

		public FiltersInSearch()
		{
			this.classID = "FiltersInSearch";
		}

		[OneTimeSetUp]
		public virtual void SetupTestClass()
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
		protected void AssertObjectIsInListing( string objectName, ListView listing )
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
		/// Set one object type as search filter.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Document",
			"Documents",
			"Mountain.jpg",
			Description = "Document object type." )]
		[TestCase(
			"Document collection",
			"Document collections",
			"Training Slides",
			Description = "Document collection object type." )]
		[TestCase(
			"Customer",
			"Customers",
			"CBH International",
			Description = "Non-document object type." )]
		public virtual void ObjectTypeFilterSearch(
			string objectType,
			string expectedHeaderText,
			string objectName )
		{

			HomePage homePage = browserManager.StartTestAtHomePage();

			SearchFilters filters = homePage.SearchPane.TypeSearchWord( objectName );

			filters.ShowMoreObjectTypeFilters();

			filters.SetFilter( objectType );

			ListView listing = homePage.SearchPane.ClickSearchButton();

			this.AssertObjectIsInListing( objectName, listing );

			List<string> headers = listing.GroupingHeaders.GroupTitles;

			Assert.AreEqual( new List<string> { expectedHeaderText }, headers,
				GroupingHeaderMismatchMessage );
		}

		/// <summary>
		/// Set two object types as search filter. Using document/document collection and a non-document object types as filter.
		/// Document and document collection grouping headers are expanded by default.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"",
			"Document",
			"Customer",
			"Documents",
			"Customers",
			"Training Slides - Day 1.ppt",
			"Reece, Murphy and Partners",
			Description = "Document + other object type, empty search." )]
		[TestCase(
			"estt",
			"Document",
			"Contact person",
			"Documents",
			"Contact persons",
			"Sales Invoice 401 - ESTT Corporation (IT).xls",
			"Sandra Williams",
			Description = "Document + other object type, with search word." )]
		[TestCase(
			"training",
			"Document collection",
			"Project",
			"Document collections",
			"Projects",
			"Training Slides",
			"Staff Training / ERP",
			Description = "Document collection + other object type, with search word" )]
		public virtual void MultipleObjectTypeFiltersSearchDocumentAndNonDocument(
			string searchWord,
			string filter1,
			string filter2,
			string header1,
			string header2,
			string object1,
			string object2 )
		{

			HomePage homePage = browserManager.StartTestAtHomePage();

			SearchFilters filters = homePage.SearchPane.TypeSearchWord( searchWord );

			filters.ShowMoreObjectTypeFilters();

			filters.SetFilter( filter1 ).SetFilter( filter2 );

			ListView listing = homePage.SearchPane.ClickSearchButton();

			// Assert that first object is already visible.
			this.AssertObjectIsInListing( object1, listing );

			// Collapse the first group.
			listing.GroupingHeaders.CollapseGroup( header1 );

			// Get all visible object type grouping headers.
			List<string> headers = listing.GroupingHeaders.GroupTitles;

			// Expand the second object type group.
			listing.GroupingHeaders.ExpandGroup( header2 );

			// Assert that the other object is also visible.
			this.AssertObjectIsInListing( object2, listing );

			// Assert that only these two headers are displayed after making the search.
			Assert.AreEqual( new List<string> { header1, header2 }, headers,
				GroupingHeaderMismatchMessage );
		}

		/// <summary>
		/// Set two object types as search filter. Using two a non-document object types as filter.
		/// Non-document object type grouping headers are collapsed by default.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"",
			"Contact person",
			"Customer",
			"Contact persons",
			"Customers",
			"Nancy Hartwick",
			"ESTT Corporation (IT)" )]
		public virtual void MultipleObjectTypeFiltersSearchTwoNonDocumentTypes(
			string searchWord,
			string filter1,
			string filter2,
			string header1,
			string header2,
			string object1,
			string object2 )
		{
			HomePage homePage = browserManager.StartTestAtHomePage();

			SearchFilters filters = homePage.SearchPane.TypeSearchWord( searchWord );

			filters.ShowMoreObjectTypeFilters();

			filters.SetFilter( filter1 ).SetFilter( filter2 );

			ListView listing = homePage.SearchPane.ClickSearchButton();

			// Expand the first group. Both non-document groups are collapsed by default.
			listing.GroupingHeaders.ExpandGroup( header1 );

			// Assert that first object is visible.
			this.AssertObjectIsInListing( object1, listing );

			// Collapse the first group after assertion.
			listing.GroupingHeaders.CollapseGroup( header1 );

			// Get all visible object type grouping headers.
			List<string> headers = listing.GroupingHeaders.GroupTitles;

			// Expand the second object type group.
			listing.GroupingHeaders.ExpandGroup( header2 );

			// Assert that the other object is also visible.
			this.AssertObjectIsInListing( object2, listing );

			// Assert that only these two headers are displayed after making the search.
			Assert.AreEqual( new List<string> { header1, header2 }, headers,
				GroupingHeaderMismatchMessage );
		}

		/// <summary>
		/// First make a search without filters. Then apply one object type filter and verify that the results are filtered.
		/// Then add another object type as search filter and verify that the search results update again to match the filter.
		/// Using document/document collection and a non-document object types as filter. Document and document collection grouping
		/// headers are expanded by default.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"",
			"Document",
			"Employee",
			"Documents",
			"Employees",
			"Training Slides - Day 3.ppt",
			"Tina Smith",
			Description = "Document + other object type, empty search" )]
		[TestCase(
			"murphy",
			"Document",
			"Project",
			"Documents",
			"Projects",
			"Order - RMP.doc",
			"CRM Application Development",
			Description = "Document + other object type, with search word" )]
		[TestCase(
			"",
			"Document collection",
			"Customer",
			"Document collections",
			"Customers",
			"Floor Plans / Central Plains",
			"RGPP Partnership",
			Description = "Document collection + other object type, empty search" )]
		public virtual void FilterSearchResultsByObjectTypeDocumentAndNonDocument(
			string searchword,
			string filter1,
			string filter2,
			string header1,
			string header2,
			string object1,
			string object2 )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Make quick search without any filter.
			ListView listing = homePage.SearchPane.QuickSearch( searchword );

			// Select filters tab.
			SearchFilters filters = homePage.TopPane.TabButtons.SearchFiltersTabClick();

			// Filter by one object type.
			filters.ShowMoreObjectTypeFilters();
			listing = filters.FilterSearchResults( filter1 );

			// Assert that expected object is visible in listing after filtering.
			this.AssertObjectIsInListing( object1, listing );

			// Collapse the object type group.
			listing.GroupingHeaders.CollapseGroup( header1 );

			// Get all visible grouping headers.
			List<string> headers = listing.GroupingHeaders.GroupTitles;

			// Assert that only the selected filter object type is visible as grouping header.
			Assert.AreEqual( new List<string> { header1 }, headers,
				GroupingHeaderMismatchMessage );

			// Add another object type filter.
			listing = filters.FilterSearchResults( filter2 );

			// Assert that still the original object should be visible from the first filter because it is
			// document or document collection type and it is expanded by default.
			this.AssertObjectIsInListing( object1, listing );

			// Collapse the first object type.
			listing.GroupingHeaders.CollapseGroup( header1 );

			// Get all visible grouping headers.
			List<string> headers2 = listing.GroupingHeaders.GroupTitles;

			// Assert that only the selected filter object types are visible as grouping headers.
			Assert.AreEqual( new List<string> { header1, header2 }, headers2,
				GroupingHeaderMismatchMessage );

			// Expand the other object type header.
			listing.GroupingHeaders.ExpandGroup( header2 );

			// Assert that an expected object type of that object type is displayed.
			this.AssertObjectIsInListing( object2, listing );

		}

		/// <summary>
		/// First make a search without filters. Then apply one object type filter and verify that the results are filtered.
		/// Then add another object type as search filter and verify that the search results update again to match the filter.
		/// Using two non-document object types as filters. Non-document grouping headers are collapsed by default.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"design",
			"Contact person",
			"Project",
			"Contact persons",
			"Projects",
			"Patrick Ellis",
			"Office Design" )]
		public virtual void FilterSearchResultsByObjectTypeTwoNonDocumentTypes(
			string searchword,
			string filter1,
			string filter2,
			string header1,
			string header2,
			string object1,
			string object2 )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Make quick search without any filter.
			ListView listing = homePage.SearchPane.QuickSearch( searchword );

			// Select filters tab.
			SearchFilters filters = homePage.TopPane.TabButtons.SearchFiltersTabClick();

			// Filter by one object type.
			filters.ShowMoreObjectTypeFilters();
			listing = filters.FilterSearchResults( filter1 );

			// Assert that expected object is visible in listing after filtering.
			this.AssertObjectIsInListing( object1, listing );

			// Collapse the object type group.
			listing.GroupingHeaders.CollapseGroup( header1 );

			// Get all visible grouping headers.
			List<string> headers = listing.GroupingHeaders.GroupTitles;

			// Assert that only the selected filter object type is visible as grouping header.
			Assert.AreEqual( new List<string> { header1 }, headers,
				GroupingHeaderMismatchMessage );

			// Add another object type filter.
			listing = filters.FilterSearchResults( filter2 );

			// Expand the first object type because both non-document object type headers 
			// are collapsed by default.
			listing.GroupingHeaders.ExpandGroup( header1 );

			// Assert that the original object should be visible from the first filter.
			this.AssertObjectIsInListing( object1, listing );

			// Collapse the first header again after the assertion.
			listing.GroupingHeaders.CollapseGroup( header1 );

			// Get all visible grouping headers.
			List<string> headers2 = listing.GroupingHeaders.GroupTitles;

			// Assert that only the selected filter object types are visible as grouping headers.
			Assert.AreEqual( new List<string> { header1, header2 }, headers2,
				GroupingHeaderMismatchMessage );

			// Expand the other object type header.
			listing.GroupingHeaders.ExpandGroup( header2 );

			// Assert that an expected object of that object type is displayed.
			this.AssertObjectIsInListing( object2, listing );

		}


		/// <summary>
		/// Search by using the filter to look only in metadata. Check that an expected object is found but also that
		/// an object is not found that has the search word in its file contents but not in metadata.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"chicago",
			"Order for Electrical Engineering.doc",
			"Sales Invoice 312 - CBH International.xls" )]
		public virtual void MetadataFilterSearch(
			string searchword,
			string expectedObj,
			string notExpectedObj )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			SearchFilters filters = homePage.SearchPane.TypeSearchWord( searchword );

			// Set to search only in metadata.
			filters.SetFilter( "Metadata" );

			ListView listing = homePage.SearchPane.ClickSearchButton();

			// Assert that an object is visible where search word matches the metadata.
			this.AssertObjectIsInListing( expectedObj, listing );

			// Assert that an object is not visible where the search word would match the file contents.
			this.AssertObjectIsNotInListing( notExpectedObj, listing );

		}

		/// <summary>
		/// Search by using the filter to look only in file contents. Check that an expected object is found but also that
		/// an object is not found that has the search word in its metadata but not in file contents.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"chicago",
			"Sales Invoice 312 - CBH International.xls",
			"Order for Electrical Engineering.doc" )]
		public virtual void FileContentsFilterSearch(
			string searchword,
			string expectedObj,
			string notExpectedObj )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			SearchFilters filters = homePage.SearchPane.TypeSearchWord( searchword );

			// Set to search only in file contents.
			filters.SetFilter( "File contents" );

			ListView listing = homePage.SearchPane.ClickSearchButton();

			// Assert that an object is visible where search word matches the file contents.
			this.AssertObjectIsInListing( expectedObj, listing );

			// Assert that an object is not visible where the search word would match the metadata.
			this.AssertObjectIsNotInListing( notExpectedObj, listing );

		}

		/// <summary>
		/// Search by using the filter to look in both file contents and metadata card. 
		/// </summary>
		[Test]
		[Category( "Search" )]
		[TestCase(
			"Metadata and File Content Keyword",
			"Documents;Customers",
			"Project Meeting Minutes 2/2006.txt;CBH International" )]
		public virtual void MetadataAndFileContentsFilterSearch(
			string searchword,
			string expectedGrpHeaders,
			string expectedObjs )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			SearchFilters filters = homePage.SearchPane.TypeSearchWord( searchword );

			// Set to search in metadata and file contents.
			filters.SetFilter( "Metadata" ).SetFilter( "File contents" );

			ListView listing = homePage.SearchPane.ClickSearchButton();

			// Store the expected grouping headers in the variable.
			List<string> expectedGroupingHeaders = StringSplitHelper.ParseStringToStringList( expectedGrpHeaders, ';' );

			// Assert that expected group headers displayed.
			Assert.AreEqual( expectedGroupingHeaders, listing.GroupingHeaders.GroupTitles,
				"Mismatch between the expected and actual grouping headers in listview." );

			// Check and expand the grouping headers if its collapsed.
			foreach( string groupHeader in expectedGroupingHeaders )
			{
				if( listing.GroupingHeaders.GetGroupStatus( groupHeader ).Equals( GroupingHeadersInListView.GroupStatus.Collapsed ) )
				{
					listing.GroupingHeaders.ExpandGroup( groupHeader );
				}
			}

			// Store the expected objects in the variable.
			List<string> expectedObjects = StringSplitHelper.ParseStringToStringList( expectedObjs, ';' );

			// Assert that the number of search results is as expected.
			Assert.AreEqual( expectedObjects.Count, listing.NumberOfItems );

			// Assert that each expected object is displayed in listing.
			foreach( string expectedObject in expectedObjects )
			{
				this.AssertObjectIsInListing( expectedObject, listing );
			}
		}

		/// <summary>
		/// First make a search without filter. The apply filter to look only in metadata. Check that an expected object 
		/// is found but also that an object is not found that has the search word in its file contents but not in metadata.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"chicago",
			"Order for Electrical Engineering.doc",
			"Sales Invoice 312 - CBH International.xls" )]
		public virtual void FilterSearchResultsByMetadata(
			string searchword,
			string expectedObj,
			string notExpectedObjAfterFilter )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Quick search without filters.
			ListView listing = homePage.SearchPane.QuickSearch( searchword );

			// Both objects should be visible in search results at this point.
			this.AssertObjectIsInListing( expectedObj, listing );
			this.AssertObjectIsInListing( notExpectedObjAfterFilter, listing );

			SearchFilters filters = homePage.TopPane.TabButtons.SearchFiltersTabClick();

			// Filter search results to show only objects with search word matching the metadata.
			listing = filters.FilterSearchResults( "Metadata" );

			// After filtering the other object should no longer match the search.
			this.AssertObjectIsInListing( expectedObj, listing );
			this.AssertObjectIsNotInListing( notExpectedObjAfterFilter, listing );
		}

		/// <summary>
		/// First make a search without filter. The apply filter to look only in file contents. Check that an expected object 
		/// is found but also that an object is not found that has the search word in its metadata but not in file contents.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"chicago",
			"Sales Invoice 312 - CBH International.xls",
			"Order for Electrical Engineering.doc" )]
		public virtual void FilterSearchResultsByFileContents(
			string searchword,
			string expectedObj,
			string notExpectedObjAfterFilter )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Quick search without filters.
			ListView listing = homePage.SearchPane.QuickSearch( searchword );

			// Both objects should be visible in search results at this point.
			this.AssertObjectIsInListing( expectedObj, listing );
			this.AssertObjectIsInListing( notExpectedObjAfterFilter, listing );

			SearchFilters filters = homePage.TopPane.TabButtons.SearchFiltersTabClick();

			// Filter search results to show only objects with search word matching the file contents.
			listing = filters.FilterSearchResults( "File contents" );

			// After filtering the other object should no longer match the search.
			this.AssertObjectIsInListing( expectedObj, listing );
			this.AssertObjectIsNotInListing( notExpectedObjAfterFilter, listing );
		}


		/// <summary>
		/// Asserts that search filter facets are displayed as expected, based on the dictionary received as parameter.
		/// Checks the facet headers and the facet items under each header. The dictionary should have facet headers
		/// as keys and the facet items as lists of strings mapped by their header key.
		/// </summary>
		/// <param name="filters">SearchFilters page object.</param>
		/// <param name="expectedFiltersDictionary">Expected search filters as a dictionary. See the summary of this method.</param>
		private void AssertAvailableSearchFilters( SearchFilters filters, Dictionary<string, List<string>> expectedFiltersDictionary )
		{
			// Get the visible search filter headers from the page object.
			List<string> actualFacetHeaders = filters.FilterFacetHeaders;

			// Assert that the number of headers is as expected.
			Assert.AreEqual( expectedFiltersDictionary.Keys.Count, actualFacetHeaders.Count );

			// Go through each expected facet header.
			for( int i = 0; i < expectedFiltersDictionary.Keys.Count; ++i )
			{
				// Get expected facet header which is used as a key in the dictionary.
				string expectedFacetHeader = expectedFiltersDictionary.Keys.ElementAt( i );

				// Assert that the header is visible.
				Assert.AreEqual( expectedFacetHeader, actualFacetHeaders.ElementAt( i ) );

				// Get all facet items under this header from the page object.
				List<string> actualFacetItems = filters.GetFilterFacetItems( expectedFacetHeader );

				// Also, get similar list from the dictionary containing the expected values.
				List<string> expectedFacetItems = expectedFiltersDictionary[ expectedFacetHeader ];

				// Assert that the number of items under this filter header matches the expected number.
				Assert.AreEqual( expectedFacetItems.Count, actualFacetItems.Count,
					"Unexpected number of items under search filter facet '" + expectedFacetHeader + "'" );

				// Go through each expected facet item.
				for( int j = 0; j < expectedFacetItems.Count; ++j )
				{
					// Assert that the expected item is displayed.
					Assert.AreEqual( expectedFacetItems.ElementAt( j ), actualFacetItems.ElementAt( j ) );
				}
			}
		}

		/// <summary>
		/// Make a search and verify that all expected search filters are then available to refine the search.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Scope:Metadata,File contents;Object type:Document,Assignment,Contact person,Customer,Document collection,Employee,Project" )]
		public virtual void AvailableFiltersInSearchResults( string expectedFilters )
		{
			// Convert test data string to dictionary.
			Dictionary<string, List<string>> expectedFiltersByHeaderName =
				StringSplitHelper.ParseStringToStringListsByKey( expectedFilters, ';', ':', ',' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Make empty search.
			ListView listing = homePage.SearchPane.QuickSearch( "" );

			// Select the filters tab and expand object type filters.
			SearchFilters filters = homePage.TopPane.TabButtons.SearchFiltersTabClick();
			filters.ShowMoreObjectTypeFilters();

			// Assert that search filters are displayed as defined in test data.
			this.AssertAvailableSearchFilters( filters, expectedFiltersByHeaderName );
		}

		/// <summary>
		/// Go to a view and then verify that all expected search filters are available to make a search in the view or to just make 
		/// a normal search.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"2. Manage Customers",
			"View:2. Manage Customers;Scope:Metadata,File contents;Object type:Document,Assignment,Contact person,Customer,Document collection,Employee,Project",
			Description = "Normal view." )]
		[TestCase(
			"1. Documents>By Class",
			"View:1. Documents > By Class;Scope:Metadata,File contents;Object type:Document,Assignment,Contact person,Customer,Document collection,Employee,Project",
			Description = "View with virtual folders." )]
		[TestCase(
			"1. Documents>By Project>IT Training",
			"View:1. Documents > By Project > IT Training;Scope:Metadata,File contents;Object type:Document,Assignment,Contact person,Customer,Document collection,Employee,Project",
			Description = "Inside virtual folder." )]
		public virtual void AvailableFiltersInView(
			string view,
			string expectedFilters )
		{
			// Convert test data string to dictionary.
			Dictionary<string, List<string>> expectedFiltersByHeaderName =
				StringSplitHelper.ParseStringToStringListsByKey( expectedFilters, ';', ':', ',' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate to a view path.
			ListView listing = homePage.ListView.NavigateToView( view );

			// Select the filters tab and expand object type filters.
			SearchFilters filters = homePage.TopPane.TabButtons.SearchFiltersTabClick();
			filters.ShowMoreObjectTypeFilters();

			// Assert that search filters are displayed as defined in test data.
			this.AssertAvailableSearchFilters( filters, expectedFiltersByHeaderName );

		}

		/// <summary>
		/// Expected search filters should be available when at home view.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase( "Scope:Metadata,File contents;Object type:Document,Assignment,Contact person,Customer,Document collection,Employee,Project" )]
		public virtual void AvailableFiltersInHome( string expectedFilters )
		{
			// Convert test data string to dictionary.
			Dictionary<string, List<string>> expectedFiltersByHeaderName =
				StringSplitHelper.ParseStringToStringListsByKey( expectedFilters, ';', ':', ',' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Select the filters tab and expand object type filters.
			SearchFilters filters = homePage.TopPane.TabButtons.SearchFiltersTabClick();
			filters.ShowMoreObjectTypeFilters();

			// Assert that search filters are displayed as defined in test data.
			this.AssertAvailableSearchFilters( filters, expectedFiltersByHeaderName );
		}


		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"2. Manage Customers",
			"Corporation",
			"ESTT Corporation (IT);OMCC Corporation",
			Description = "Normal view." )]
		[TestCase(
			"1. Documents>By Project>IT Training",
			"slides",
			"Training Slides - Day 1.ppt;Training Slides - Day 2.ppt;Training Slides - Day 3.ppt;Training Slides - Day 4.ppt",
			Description = "In virtual folder." )]
		[TestCase(
			"1. Documents>By Class Group and Class>4. MEETINGS>Memo",
			"2006",
			"Project Meeting Minutes 1/2006.txt;Project Meeting Minutes 2/2006.txt",
			Description = "In a nested virtual folder with multiple levels." )]
		public virtual void SearchWithinView(
			string view,
			string searchWord,
			string expectedObjectsString )
		{
			List<string> expectedObjects = StringSplitHelper.ParseStringToStringList( expectedObjectsString, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Go to view.
			ListView listing = homePage.ListView.NavigateToView( view );

			// Enter search word and set filter to search within the current view.
			SearchFilters filters = homePage.SearchPane.TypeSearchWord( searchWord );
			filters.SetSearchWithinThisViewFilter();

			// Make the search.
			listing = homePage.SearchPane.ClickSearchButton();

			// Assert that each expected object is displayed in listing.
			Assert.AreEqual( expectedObjects, listing.ItemNames,
				"Mismatch between expected and actual objects in listing." );
		}
	}
}
