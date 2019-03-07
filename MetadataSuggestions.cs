using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Motive.MFiles.vNextUI.Tests
{
	/// <summary>
	/// Tests related to metadata suggestions by using M-Files Matcher.
	/// </summary>
	[Order( -6 )]
	[Parallelizable( ParallelScope.Self )]
	public class MetadataSuggestions
	{
		/// <summary>
		/// Test class identifier that is used to identify configurations for this class.
		/// </summary>
		protected readonly string classID = "MetadataSuggestions";

		/// <summary>
		/// The configurations for the test class.
		/// </summary>
		private TestClassConfiguration configuration;

		/// <summary>
		/// The browser manager for test class.
		/// </summary>
		private TestClassBrowserManager browserManager;

		/// <summary>
		/// Test context information, including vault connections.
		/// </summary>
		private MFilesContext mfContext;

		/// <summary>
		/// The name of the test vault.
		/// </summary>
		private string VaultName => this.mfContext.VaultName;

		/// <summary>
		/// The username of the default user.
		/// </summary>
		private string Username => this.mfContext.UsernameOfUser( "user" );

		/// <summary>
		/// The password of the default user.
		/// </summary>
		private string Password => this.mfContext.PasswordOfUser( "user" );

		/// <summary>
		/// Initializes the test environment.
		/// </summary>
		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsersWithSystemAdmin();

			// Initialize the test environment.
			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment(
					EnvironmentHelper.VaultBackend.Firebird,
					"Data Types And Test Objects.mfb",
					users );

			// Install Information Extractor.
			EnvironmentHelper.InstallInformationExtractor( this.mfContext );

			// Configure M-Files Matcher.
			string customerObjectTypeGuid = "{625B5783-4A71-45A3-BE6A-901331C72930}";
			string customerPropertyDefGuid = "{637261E8-F25C-4C26-99F4-EE002BDCB222}";
			string departmentValueListGuid = "{97BC027F-96AB-458C-A1FD-A25A038E7BFE}";
			string departmentPropertyDefGuid = "{7640F94E-73F9-40A8-83FB-C96E889B2D23}";
			EnvironmentHelper.ConfigureMatcher( this.mfContext[ "admin" ],
					Tuple.Create( customerObjectTypeGuid, customerPropertyDefGuid ),
					Tuple.Create( departmentValueListGuid, departmentPropertyDefGuid ) );

			// Restart the vault.
			EnvironmentSetupHelper.RestartVault( this.mfContext );

			// Initialize browser manager with default users.
			this.browserManager = new TestClassBrowserManager(
					this.configuration,
					this.Username,
					this.Password,
					this.VaultName );
		}

		/// <summary>
		/// Tear downs the test environment.
		/// </summary>
		[OneTimeTearDown]
		public void TeardownTestClass()
		{
			// Quit the existing browser.
			this.browserManager.EnsureQuitBrowser();

			EnvironmentSetupHelper.TearDownEnvironment( this.mfContext );
		}

		/// <summary>
		/// Finalize browser state after test is failed or succeed.
		/// </summary>
		[TearDown]
		public void EndTest()
		{
			this.browserManager.FinalizeBrowserStateBasedOnTestResult( TestExecutionContext.CurrentContext );
		}

		/// <summary>
		/// Tests that analyze button works and returns correct property and value suggestions.
		/// </summary>
		/// <param name="objectType">The object type.</param>
		/// <param name="objectName">The object name</param>
		/// <param name="suggestions">Expected suggestions.</param>
		[TestCase(
			"Document",
			"Matcher - Two suggestions to new properties.txt",
			"Customer:Fortney Nolte Associates,CBH International;" +
			"Department:Administration,Marketing" )]
		public void AnalyzeMetadataSuggestions(
			string objectType,
			string objectName,
			string suggestions
		)
		{
			// Parse expected suggestions.
			Dictionary<string, List<string>> expectedSuggestions
				= StringSplitHelper.ParseStringToStringListsByKey( suggestions );

			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			// At first the analysis status should be seen as not executed.
			Assert.AreEqual( AnalysisStatus.NotExecuted, metadataCardRightPane.MetadataSuggestion.AnalysisStatus,
					"Analysis status is incorrect immediately after metadata card is opened." );

			// Analyze property suggestion.
			AnalysisStatus currentAnalysisStatus = metadataCardRightPane.MetadataSuggestion.Analyze();

			// Because object should have metadata suggestions the analysis status should
			// be analysis complete.
			Assert.AreEqual( AnalysisStatus.AnalysisComplete, currentAnalysisStatus,
					"Analysis status is incorrect after it is executed." );

			// Object should have expected property suggestions.
			List<string> propertySuggestions = metadataCardRightPane.MetadataSuggestion.PropertySuggestions;
			Assert.That( propertySuggestions, Is.EquivalentTo( expectedSuggestions.Keys ),
					"Invalid properties are suggested." );

			// Object should not have the suggested properties in metadata card.
			foreach( string propertySuggestion in expectedSuggestions.Keys )
				Assert.IsFalse( metadataCardRightPane.Properties.IsPropertyInMetadataCard( propertySuggestion ),
						$"Suggested {propertySuggestion} property is in metadatacard before it is selected." );

			// Select all property suggestions. All property suggestions should 
			// disappear after they are selected.
			foreach( string propertySuggestion in expectedSuggestions.Keys )
			{
				metadataCardRightPane.MetadataSuggestion.SelectPropertySuggestion( propertySuggestion );
				Assert.That( metadataCardRightPane.MetadataSuggestion.PropertySuggestions, Does.Not.Contain( propertySuggestion ),
						"Selected property suggestion didn't disappear from metadata card." );
			}

			// Object should now have selected property suggestions as property in metadata card.
			List<string> currentProperties = metadataCardRightPane.Properties.PropertyNames;
			foreach( string propertySuggestion in expectedSuggestions.Keys )
				Assert.That( currentProperties, Contains.Item( propertySuggestion ) );

			// After both property suggestions are selected there should not be anymore
			// property suggestions visible in metadata card
			Assert.IsEmpty( metadataCardRightPane.MetadataSuggestion.PropertySuggestions,
					"There is property suggestions in metadata card after all of them are selected." );

			// Test that all properties has correct value suggestions.
			foreach( KeyValuePair<string, List<string>> suggestion in expectedSuggestions )
			{
				List<string> valueSuggestions = metadataCardRightPane.MetadataSuggestion.GetValueSuggestions( suggestion.Key );
				Assert.That( valueSuggestions, Is.EquivalentTo( suggestion.Value ),
						$"{suggestion.Key} property has invalid value suggestions" );
			}
		}

		/// <summary>
		/// Tests that analyze button works in popout metadata card and returns correct property and value suggestions.
		/// </summary>
		/// <param name="objectType">The object type.</param>
		/// <param name="objectName">The object name</param>
		/// <param name="suggestions">Expected suggestions.</param>
		[TestCase(
			"Document",
			"Matcher - Two suggestions to new properties.txt",
			"Customer:Fortney Nolte Associates,CBH International;" +
			"Department:Administration,Marketing" )]
		public void AnalyzeMetadataSuggestionsInPopoutMetadataCard(
			string objectType,
			string objectName,
			string suggestions
		)
		{
			// Parse expected suggestions.
			Dictionary<string, List<string>> expectedSuggestions
				= StringSplitHelper.ParseStringToStringListsByKey( suggestions );

			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			// Pop out metadata card.
			MetadataCardPopout metadataCardPopout = metadataCardRightPane.PopoutMetadataCard();

			// At first the analysis status should be seen as not executed.
			Assert.AreEqual( AnalysisStatus.NotExecuted, metadataCardPopout.MetadataSuggestion.AnalysisStatus,
					"Analysis status is incorrect immediately after metadata card is opened." );

			// Analyze property suggestion.
			AnalysisStatus currentAnalysisStatus = metadataCardPopout.MetadataSuggestion.Analyze();

			// Because object should have metadata suggestions the analysis status should
			// be analysis complete.
			Assert.AreEqual( AnalysisStatus.AnalysisComplete, currentAnalysisStatus,
					"Analysis status is incorrect after it is executed." );

			// Object should have expected property suggestions.
			List<string> propertySuggestions = metadataCardPopout.MetadataSuggestion.PropertySuggestions;
			Assert.That( propertySuggestions, Is.EquivalentTo( expectedSuggestions.Keys ),
					"Invalid properties are suggested." );

			// Object should not have the suggested properties in metadata card.
			foreach( string propertySuggestion in expectedSuggestions.Keys )
				Assert.IsFalse( metadataCardPopout.Properties.IsPropertyInMetadataCard( propertySuggestion ),
						$"Suggested {propertySuggestion} property is in metadatacard before it is selected." );

			// Select all property suggestions. All property suggestions should 
			// disappear after they are selected.
			foreach( string propertySuggestion in expectedSuggestions.Keys )
			{
				metadataCardPopout.MetadataSuggestion.SelectPropertySuggestion( propertySuggestion );
				Assert.That( metadataCardPopout.MetadataSuggestion.PropertySuggestions, Does.Not.Contain( propertySuggestion ),
						"Selected property suggestion didn't disappear from metadata card." );
			}

			// Object should now have selected property suggestions in metadata card.
			List<string> currentProperties = metadataCardPopout.Properties.PropertyNames;
			foreach( string propertySuggestion in expectedSuggestions.Keys )
				Assert.That( currentProperties, Contains.Item( propertySuggestion ) );

			// After both property suggestions are selected there should not be anymore
			// property suggestions visible in metadata card
			Assert.IsEmpty( metadataCardPopout.MetadataSuggestion.PropertySuggestions,
					"There is property suggestions in metadata card after all of them are selected." );

			// Test that all properties has correct value suggestions.
			foreach( KeyValuePair<string, List<string>> suggestion in expectedSuggestions )
			{
				List<string> valueSuggestions = metadataCardPopout.MetadataSuggestion.GetValueSuggestions( suggestion.Key );
				Assert.That( valueSuggestions, Is.EquivalentTo( suggestion.Value ),
						$"{suggestion.Key} property has invalid value suggestions" );
			}

			// Close the popout metadata card.
			metadataCardPopout.CloseButtonClick();
		}

		/// <summary>
		/// Tests that Analyse button, property suggestions and values suggestions work correctly in 
		/// the metadata card in right pane.
		/// </summary>
		/// <param name="objectType">The object type</param>
		/// <param name="objectName">The object name</param>
		/// <param name="suggestionsForMSLUs"></param>
		/// <param name="suggestionsForSSLUs"></param>
		[TestCase(
			"Document",
			"Matcher - Two suggestions to new properties 2.txt",
			"Customer:Fortney Nolte Associates,CBH International",
			"Department:Administration,Marketing" )]
		public void SelectValueSuggestionsInRightPaneMetadataCard(
			string objectType,
			string objectName,
			string suggestionsForMSLUs,
			string suggestionsForSSLUs
		)
		{
			// Parse expected suggestions. Keys informs the properties that are suggested and
			// list informs all value suggestions per property.
			Dictionary<string, List<string>> expectedSuggestionsMSLU
					= StringSplitHelper.ParseStringToStringListsByKey( suggestionsForMSLUs );
			Dictionary<string, List<string>> expectedSuggestionsSSLU
					= StringSplitHelper.ParseStringToStringListsByKey( suggestionsForSSLUs );
			Dictionary<string, List<string>> expectedSuggestions = new Dictionary<string, List<string>>();
			foreach( var dict in new[] { expectedSuggestionsMSLU, expectedSuggestionsSSLU } )
				foreach( var kvp in dict )
					expectedSuggestions.Add( kvp.Key, kvp.Value );

			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCard = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			// Analyze property suggestion.
			metadataCard.MetadataSuggestion.Analyze();

			// Select all property suggestions. All property suggestions should 
			// disappear after they are selected.
			foreach( string propertySuggestion in expectedSuggestions.Keys )
			{
				metadataCard.MetadataSuggestion.SelectPropertySuggestion( propertySuggestion );
				Assert.That( metadataCard.MetadataSuggestion.PropertySuggestions, Does.Not.Contain( propertySuggestion ),
						"Selected property suggestion didn't disappear from metadata card." );
			}

			// Select value suggestions for multi-select lookup property.

			// Test value suggestions only with the first multi-select lookup that 
			// is given as test case parameter.
			KeyValuePair<string, List<string>> msluPropertyWithExpectedSuggestions = expectedSuggestionsMSLU.First();
			string msluProperty = msluPropertyWithExpectedSuggestions.Key;
			List<string> expectedValueSuggestionsForMslu = new List<string>( msluPropertyWithExpectedSuggestions.Value );
			List<string> expectedValuesForMslu = new List<string>();

			// Select all suggested values for multi-select lookup one by one and ensure
			// that the metadata card is updated correctly.
			foreach( string valueSuggestion in msluPropertyWithExpectedSuggestions.Value )
			{
				// Select the value suggestions and update expected value suggestion and values in the
				// property.
				metadataCard.MetadataSuggestion.SelectValueSuggestion( msluProperty, valueSuggestion );
				expectedValueSuggestionsForMslu.Remove( valueSuggestion );
				expectedValuesForMslu.Add( valueSuggestion );

				// Value suggestion should disappear from suggested values.
				List<string> currentValueSuggestions = metadataCard.MetadataSuggestion.GetValueSuggestions( msluProperty );
				Assert.That( currentValueSuggestions, Is.EquivalentTo( expectedValueSuggestionsForMslu ),
						$"{msluProperty} property has invalid value suggestions" );

				// Selected value suggestion should now be added to multi-select lookup as last one.
				List<string> customers = metadataCard.Properties.GetMultiSelectLookupPropertyValues( msluProperty );
				Assert.That( customers, Is.EqualTo( expectedValuesForMslu ),
						"A value suggestion that is selected is not added correctly to multi-select lookup." );
			}

			// Select value suggestions for single select lookup property.

			// Test value suggestions only with the first single-select lookup that 
			// is given as test case parameter.
			KeyValuePair<string, List<string>> ssluPropertyWithExpectedSuggestions = expectedSuggestionsSSLU.First();
			string ssluProperty = ssluPropertyWithExpectedSuggestions.Key;
			List<string> expectedValueSuggestionsForSslu = new List<string>( ssluPropertyWithExpectedSuggestions.Value );
			string expectedValueForSslu = "";

			// Select first two value suggestions to single-select lookup and ensure
			// that the metadata card is updated correctly.
			foreach( string valueSuggestion in ssluPropertyWithExpectedSuggestions.Value.Take( 2 ) )
			{
				// Select value suggestion and update expected values. Selected value suggestion
				// should overwrite the old value.
				metadataCard.MetadataSuggestion.SelectValueSuggestion( ssluProperty, valueSuggestion );
				expectedValueSuggestionsForSslu.Remove( valueSuggestion );
				expectedValueForSslu = valueSuggestion;

				// Selected value should disappear from value suggestions.
				List<string> valueSuggestions = metadataCard.MetadataSuggestion.GetValueSuggestions( ssluProperty );
				Assert.That( valueSuggestions, Is.EquivalentTo( expectedValueSuggestionsForSslu ),
						"Property has invalid value suggestions" );

				// Selected value should be set to the property.
				Assert.AreEqual( expectedValueForSslu, metadataCard.Properties.GetPropertyValue( ssluProperty ),
						"Incorrect property value in single-select lookup after value suggestion is selected." );
			}

			// Save changes.
			metadataCard = metadataCard.SaveAndDiscardOperations.Save();

			// Object should still have properties that were added from property suggestions.
			List<string> properties = metadataCard.Properties.PropertyNames;
			Assert.That( properties, Contains.Item( msluProperty ),
					"Property is vanished after selected property suggestions are save " );
			Assert.That( properties, Contains.Item( ssluProperty ),
					"Property is vanished after selected property suggestions are save " );

			// Customer should have same values as before saving.
			List<string> lookups = metadataCard.Properties.GetMultiSelectLookupPropertyValues( msluProperty );
			Assert.That( lookups, Is.EquivalentTo( expectedValuesForMslu ),
					$"{msluProperty} property has invalid values after selected metadata suggestions are saved." );

			// Department property should have same value as before saving
			Assert.AreEqual( expectedValueForSslu, metadataCard.Properties.GetPropertyValue( ssluProperty ),
					$"{ssluProperty} property has invalid value after selected metadata suggestions are saved." );
		}

		/// <summary>
		/// Tests that Analyse button, property suggestions and values suggestions work correctly in 
		/// the metadata card in right pane.
		/// </summary>
		/// <param name="objectType">The object type</param>
		/// <param name="objectName">The object name</param>
		[TestCase(
			"Document",
			"Matcher - Two suggestions to new properties 3.txt",
			"Customer:Fortney Nolte Associates,CBH International",
			"Department:Administration,Marketing" )]
		public void SelectValueSuggestionsInPopoutMetadataCard(
			string objectType,
			string objectName,
			string suggestionsForMSLUs,
			string suggestionsForSSLUs
		)
		{
			// Parse expected suggestions. Keys informs the properties that are suggested and
			// list is inform all value suggestions per property.
			Dictionary<string, List<string>> expectedSuggestionsMSLU
					= StringSplitHelper.ParseStringToStringListsByKey( suggestionsForMSLUs );
			Dictionary<string, List<string>> expectedSuggestionsSSLU
					= StringSplitHelper.ParseStringToStringListsByKey( suggestionsForSSLUs );
			Dictionary<string, List<string>> expectedSuggestions = new Dictionary<string, List<string>>();
			foreach( var dict in new[] { expectedSuggestionsMSLU, expectedSuggestionsSSLU } )
				foreach( var kvp in dict )
					expectedSuggestions.Add( kvp.Key, kvp.Value );

			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			// Pop out metadata card.
			MetadataCardPopout metadataCardPopout = metadataCardRightPane.PopoutMetadataCard();

			// Analyze property suggestion.
			metadataCardPopout.MetadataSuggestion.Analyze();

			// Select both property suggestions. Both property suggestions should 
			// disappear after they are selected.
			foreach( string propertySuggestion in expectedSuggestions.Keys )
			{
				metadataCardPopout.MetadataSuggestion.SelectPropertySuggestion( propertySuggestion );
				Assert.That( metadataCardPopout.MetadataSuggestion.PropertySuggestions, Does.Not.Contain( propertySuggestion ),
						"Selected property suggestion didn't disappear from metadata card." );
			}

			// Select value suggestions for multi-select lookup property.

			// Test value suggestions only with the first multi-select lookup that 
			// is given as test case parameter.
			KeyValuePair<string, List<string>> msluPropertyWithExpectedSuggestions = expectedSuggestionsMSLU.First();
			string msluProperty = msluPropertyWithExpectedSuggestions.Key;
			List<string> expectedValueSuggestionsForMslu = new List<string>( msluPropertyWithExpectedSuggestions.Value );
			List<string> expectedValuesForMslu = new List<string>();

			// Select all suggested values for multi-select lookup one by one and ensure
			// that the metadata card is updated correctly.
			foreach( string valueSuggestion in msluPropertyWithExpectedSuggestions.Value )
			{
				// Select the value suggestions and update expected value suggestion and values in the
				// property.
				metadataCardPopout.MetadataSuggestion.SelectValueSuggestion( msluProperty, valueSuggestion );
				expectedValueSuggestionsForMslu.Remove( valueSuggestion );
				expectedValuesForMslu.Add( valueSuggestion );

				// Value suggestion should disappear from suggested values.
				List<string> currentValueSuggestions = metadataCardPopout.MetadataSuggestion.GetValueSuggestions( msluProperty );
				Assert.That( currentValueSuggestions, Is.EquivalentTo( expectedValueSuggestionsForMslu ),
						$"{msluProperty} property has invalid value suggestions" );

				// Selected value suggestion should now be added to multi-select lookup as last one.
				List<string> customers = metadataCardPopout.Properties.GetMultiSelectLookupPropertyValues( msluProperty );
				Assert.That( customers, Is.EqualTo( expectedValuesForMslu ),
						"A value suggestion that is selected is not added correctly to multi-select lookup." );
			}

			// Select value suggestions for single select lookup property.

			// Test value suggestions only with the first single-select lookup that 
			// is given as test case parameter.
			KeyValuePair<string, List<string>> ssluPropertyWithExpectedSuggestions = expectedSuggestionsSSLU.First();
			string ssluProperty = ssluPropertyWithExpectedSuggestions.Key;
			List<string> expectedValueSuggestionsForSslu = new List<string>( ssluPropertyWithExpectedSuggestions.Value );
			string expectedValueForSslu = "";

			// Select first two value suggestions to single-select lookup and ensure
			// that the metadata card is updated correctly.
			foreach( string valueSuggestion in ssluPropertyWithExpectedSuggestions.Value.Take( 2 ) )
			{
				// Select value suggestion and update expected values. Selected value suggestion
				// should overwrite the old value.
				metadataCardPopout.MetadataSuggestion.SelectValueSuggestion( ssluProperty, valueSuggestion );
				expectedValueSuggestionsForSslu.Remove( valueSuggestion );
				expectedValueForSslu = valueSuggestion;

				// Selected value should disappear from value suggestions.
				List<string> valueSuggestions = metadataCardPopout.MetadataSuggestion.GetValueSuggestions( ssluProperty );
				Assert.That( valueSuggestions, Is.EquivalentTo( expectedValueSuggestionsForSslu ),
						"Property has invalid value suggestions" );

				// Selected value should be set to the property.
				Assert.AreEqual( expectedValueForSslu, metadataCardPopout.Properties.GetPropertyValue( ssluProperty ),
						"Incorrect property value in single-select lookup after value suggestion is selected." );
			}

			// Save changes.
			metadataCardRightPane = metadataCardPopout.SaveAndDiscardOperations.Save();

			// Object should still have properties that were added from property suggestions.
			List<string> properties = metadataCardRightPane.Properties.PropertyNames;
			Assert.That( properties, Contains.Item( msluProperty ),
					"Property is vanished after selected property suggestions are save " );
			Assert.That( properties, Contains.Item( ssluProperty ),
					"Property is vanished after selected property suggestions are save " );

			// Customer should have same values as before saving.
			List<string> lookups = metadataCardRightPane.Properties.GetMultiSelectLookupPropertyValues( msluProperty );
			Assert.That( lookups, Is.EquivalentTo( expectedValuesForMslu ),
					$"{msluProperty} property has invalid values after selected metadata suggestions are saved." );

			// Department property should have same value as before saving
			Assert.AreEqual( expectedValueForSslu, metadataCardRightPane.Properties.GetPropertyValue( ssluProperty ),
					$"{ssluProperty} property has invalid value after selected metadata suggestions are saved." );
		}

		/// <summary>
		/// Tests a situations when metadata suggestions are analysed in situations when
		/// object already have suggested properties and values in metadata card.
		/// </summary>
		/// <param name="objectType">The object type</param>
		/// <param name="objectName">The object name</param>
		[TestCase(
			"Document",
			"Matcher - Multiple suggestions.txt" )]
		public void ExistingPropertiesAndValues(
			string objectType,
			string objectName
		)
		{
			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCard = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			// Analyze metadata suggestions.
			metadataCard.MetadataSuggestion.Analyze();

			// Select one property suggestion and first value that its suggested.
			string selectedProperty = "Customer";
			metadataCard.MetadataSuggestion.SelectPropertySuggestion( selectedProperty );
			string selectedValue = metadataCard.MetadataSuggestion.GetValueSuggestions( selectedProperty ).First();
			metadataCard.MetadataSuggestion.SelectValueSuggestion( selectedProperty, selectedValue );

			// Save changes. After that the object has one property and one value 
			// already and these should not be suggested anymore.
			metadataCard = metadataCard.SaveAndDiscardOperations.Save();

			// Object already has one property and value that is going to be suggested.

			// Analyze metadata suggestions again.
			metadataCard.MetadataSuggestion.Analyze();

			// Selected property should not be suggested anymore because it is already in metadata card
			Assert.That( metadataCard.MetadataSuggestion.PropertySuggestions, Does.Not.Contain( selectedProperty ),
					"Property that is already in metadata card is suggested." );

			// Selected property should have value suggestions but there should not be
			// the value that is already selected in the metadata card.
			List<string> valueSuggestions = metadataCard.MetadataSuggestion.GetValueSuggestions( selectedProperty );
			Assert.IsNotEmpty( valueSuggestions,
					"Metadata card does not show value suggestions or property that is already in metadata card." );
			Assert.That( valueSuggestions, Does.Not.Contain( selectedValue ),
					"Property value that is already selected is in suggestions." );

			// Select all suggested property values.
			foreach( string value in valueSuggestions )
				metadataCard.MetadataSuggestion.SelectValueSuggestion( selectedProperty, value );

			// Save changes.
			metadataCard = metadataCard.SaveAndDiscardOperations.Save();

			// All values that is going to be suggested are already selected.

			// Analyze metadata suggestions again.
			metadataCard.MetadataSuggestion.Analyze();

			// The property should not have any value suggestions.
			Assert.IsEmpty( metadataCard.MetadataSuggestion.GetValueSuggestions( selectedProperty ),
					"The object has value suggestions even if all values that is going to be suggested is already selected." );

			// Remove one value from multi-select lookup.
			string onePropertyValue = metadataCard.Properties.GetMultiSelectLookupPropertyValues( selectedProperty ).First();
			metadataCard.Properties.RemoveMultiSelectLookupPropertyValue( selectedProperty, onePropertyValue );

			// Analyze metadata suggestions.
			metadataCard.MetadataSuggestion.Analyze();

			// Verify that the removed property value is suggested.
			Assert.That(
					metadataCard.MetadataSuggestion.GetValueSuggestions( selectedProperty ),
					Contains.Item( onePropertyValue ) );

			// Remove the property from metadata card.
			metadataCard.Properties.RemoveProperty( selectedProperty );

			// Analyze metadata suggestions.
			metadataCard.MetadataSuggestion.Analyze();

			// Verify that removed property is suggested.
			Assert.That( metadataCard.MetadataSuggestion.PropertySuggestions, Contains.Item( selectedProperty ) );
		}

		/// <summary>
		/// Tests that analyzed metadata suggestions are transfered correctly to pop out metadata card.
		/// </summary>
		/// <param name="objectType">The object type.</param>
		/// <param name="objectName">The object name</param>
		[TestCase(
			"Document",
			"Matcher - Two suggestions to new properties.txt" )]
		public void AnalysisStatusIsTransferedToPopoutMetadataCard(
			string objectType,
			string objectName
		)
		{
			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			// Analyze metadata suggestions..
			AnalysisStatus analysisStatusRightPane = metadataCardRightPane.MetadataSuggestion.Analyze();

			// Select first property suggestion.
			string propertySuggestion = metadataCardRightPane.MetadataSuggestion.PropertySuggestions.First();
			metadataCardRightPane.MetadataSuggestion.SelectPropertySuggestion( propertySuggestion );

			// Retrieve current property suggestions and value suggestions of selected value suggestion.
			List<string> propertySuggestionsRightPane = metadataCardRightPane.MetadataSuggestion.PropertySuggestions;
			List<string> valueSuggestionsRightPane = metadataCardRightPane.MetadataSuggestion.GetValueSuggestions( propertySuggestion );

			// Pop out metadata card.
			MetadataCardPopout metadataCardPopout = metadataCardRightPane.PopoutMetadataCard();

			// Retrieve analyzation information from the popout metadata card.
			AnalysisStatus analysisStatusPopout = metadataCardPopout.MetadataSuggestion.AnalysisStatus;
			List<string> propertySuggestionsPopout = metadataCardPopout.MetadataSuggestion.PropertySuggestions;
			List<string> valueSuggestionsPopout = metadataCardPopout.MetadataSuggestion.GetValueSuggestions( propertySuggestion );

			// Verify that metadata suggestion analysis status has remain same after the metadata card is popped out.
			Assert.AreEqual( analysisStatusRightPane, analysisStatusPopout,
					"Analysis status is not transfered to the popout metadata card." );
			Assert.That( propertySuggestionsRightPane, Is.EqualTo( propertySuggestionsPopout ),
					"Property suggestion are not transfered to the popout metadata card." );
			Assert.That( valueSuggestionsRightPane, Is.EqualTo( valueSuggestionsPopout ),
					"Property suggestion are not transfered to the popout metadata card." );

			// Close popout metadata card.
			metadataCardPopout.CloseButtonClick();
		}

		/// <summary>
		/// Tests that analysis status is shown correctly when there isn't any suggestions.
		/// </summary>
		/// <param name="objectType">The object type</param>
		/// <param name="objectName">The object name</param>
		[TestCase(
			"Document",
			"Project Plan / Feasibility Study.doc" )]
		public void ObjectWithNoSuggestions(
			string objectType,
			string objectName
		)
		{
			// Ensure that home page is visible.
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Open metadatacard of the test object.
			MetadataCardRightPane metadataCardRightPane = homePage.SearchPane
						.FilteredQuickSearch( objectName, objectType )
						.SelectObject( objectName );

			// Analyze property suggestion.
			AnalysisStatus currentAnalysisStatus = metadataCardRightPane.MetadataSuggestion.Analyze();

			// Because object should have metadata suggestions the analysis status should
			// be analysis complete.
			Assert.AreEqual( AnalysisStatus.NoSuggestions, currentAnalysisStatus,
					"Analysis status is shown incorrect when there aren't any suggestions." );

			// Popout metadata card.
			MetadataCardPopout metadataCardPopout = metadataCardRightPane.PopoutMetadataCard();

			// Analyze property suggestion.
			currentAnalysisStatus = metadataCardPopout.MetadataSuggestion.Analyze();

			// Because object should have metadata suggestions the analysis status should
			// be analysis complete.
			Assert.AreEqual( AnalysisStatus.NoSuggestions, currentAnalysisStatus,
					"Analysis status is shown incorrect when there aren't any suggestions." );

			// Close popout metadata card.
			metadataCardPopout.CloseButtonClick();
		}
	}
}
