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
	[Order( -1 )]
	[Parallelizable( ParallelScope.Self )]
	class NavigationBreadcrumb
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID;

		private static readonly string BreadCrumbMismatchMessage = "Mismatch between expected and actual breadcrumb.";

		private string username;
		private string password;
		private string vaultName;

		private TestClassConfiguration configuration;

		private MFilesContext mfContext;

		private TestClassBrowserManager browserManager;

		public NavigationBreadcrumb()
		{
			this.classID = "NavigationBreadcrumb";
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

		[Test]
		[Category( "Smoke" )]
		[TestCase( "Project Schedule.pdf", "1. Documents" )]
		public void NavigateHomeFromSearchByBreadCrumb( string searchObject, string controlView )
		{

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.QuickSearch( searchObject );

			// Expected breadcrumb in search view has the vault name, and "Search" string as 
			// the last item in breadcrumb
			List<string> expectedBreadCrumbInSearchResultsView =
				new List<string> { this.vaultName, $"Search: {searchObject}" };

			// Assert that the breadcrumb is as expected.
			Assert.AreEqual( expectedBreadCrumbInSearchResultsView, homePage.TopPane.BreadCrumb,
				BreadCrumbMismatchMessage );

			// Assert that an object is visible in search results.
			Assert.True( listing.IsItemInListing( searchObject ) );

			homePage.TopPane.NavigateToBreadCrumbItem( this.vaultName );

			// Assert the breadcrumb containing only the vault name.
			Assert.AreEqual( new List<string> { this.vaultName }, homePage.TopPane.BreadCrumb,
				BreadCrumbMismatchMessage );

			// Verify that some view is visible which should be visible only in home view.
			// This provides assurance that navigation to home view was successful.
			Assert.True( listing.IsItemInListing( controlView ) );

		}

		[Test]
		[Category( "Smoke" )]
		[TestCase( "1. Documents>By Class>Picture", "Parrot.jpg", "1. Documents" )]
		public void NavigateHomeFromViewByBreadCrumb( string viewPath, string controlObject, string controlView )
		{

			List<string> viewSteps = StringSplitHelper.ParseStringToStringList( viewPath, '>' );

			// Add vault name as the first item, because that is the first part of the expected breadcrumb.
			viewSteps.Insert( 0, this.vaultName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate through necessary view levels one by one to reach the target view.
			ListView listing = homePage.ListView.NavigateToView( viewPath );

			// Verify the whole breadcrumb.
			Assert.AreEqual( viewSteps, homePage.TopPane.BreadCrumb,
				BreadCrumbMismatchMessage );

			// Assert that some object in the view is visible.
			Assert.True( listing.IsItemInListing( controlObject ) );

			// Navigate back to home view by using the vault name in the breadcrumb.
			homePage.TopPane.NavigateToBreadCrumbItem( this.vaultName );

			// Verify that breadcrumb contains only the vault name.
			Assert.AreEqual( new List<string> { this.vaultName }, homePage.TopPane.BreadCrumb,
				BreadCrumbMismatchMessage );

			// Verify that some view is visible which should be visible only in home view.
			// This provides assurance that navigation to home view was successful.
			Assert.True( listing.IsItemInListing( controlView ) );
		}


		[Test]
		[Category( "Smoke" )]
		[TestCase( "2. Manage Customers", Description = "Simple view with a filter." )]
		[TestCase( "1. Documents>By Class>Financial Statement", Description = "View containing another view which has virtual folders." )]
		[TestCase( "1. Documents>By Class>Picture", Description = "View containing another view which has virtual folder converted to view." )]
		public void BacktrackViewStepsByBreadCrumb( string viewPath )
		{

			List<string> viewSteps = StringSplitHelper.ParseStringToStringList( viewPath, '>' );

			// Add vault name as the first item, because that is the first part of the expected breadcrumb.
			viewSteps.Insert( 0, this.vaultName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Navigate through necessary view levels one by one to reach the target view.
			ListView listing = homePage.ListView.NavigateToView( viewPath );

			// Verify the whole breadcrumb.
			List<string> expectedBreadCrumb = new List<string>( viewSteps );
			Assert.AreEqual( expectedBreadCrumb, homePage.TopPane.BreadCrumb, BreadCrumbMismatchMessage );

			// Start going through the view steps backwards. Starting from the view index where the application currently is.
			for( int currentlyInViewIndex = viewSteps.Count - 1; currentlyInViewIndex >= 1; --currentlyInViewIndex )
			{

				// Navigate to previous view level.
				homePage.TopPane.NavigateToBreadCrumbItem( viewSteps.ElementAt( currentlyInViewIndex - 1 ) );

				// Now when navigated one step back, let's just call the "currently in view index" as "previously in view index"
				// because that is now the index of the view that should no longer be displayed in the breadcrumb.
				int previouslyInViewIndex = currentlyInViewIndex;

				// Now the previous view should be in listing because application was navigated one level up the breadcrumb.
				Assert.True( listing.IsItemInListing( viewSteps.ElementAt( previouslyInViewIndex ) ) );

				// Remove the last item from expected breadcrumb because user navigated up one level.
				expectedBreadCrumb.RemoveAt( expectedBreadCrumb.Count - 1 );

				// Assert that the breadcrumb is as expected.
				Assert.AreEqual( expectedBreadCrumb, homePage.TopPane.BreadCrumb, BreadCrumbMismatchMessage );

			}
		}

	}
}
