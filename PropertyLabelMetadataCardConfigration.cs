using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests.MetadataCardConfiguration
{
	[Order( -8 )]
	[Parallelizable( ParallelScope.Self )]
	class PropertyLabelMetadataCardConfigration
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

		public PropertyLabelMetadataCardConfigration()
		{
			this.classID = "PropertyLabelMetadataCardConfigration";
		}

		/// <summary>
		/// Asserts that expected value is visible in the property. Provides error message
		/// if assertion fails.
		/// </summary>
		/// <param name="expectedValue">Expected property value.</param>
		/// <param name="setValueProperty">Property name.</param>
		/// <param name="properties">Properties page object.</param>
		private void AssertSetValueProperty(
			string expectedValue,
			string setValueProperty,
			PropertiesInMetadataCard properties )
		{
			Assert.AreEqual( expectedValue, properties.GetPropertyValue( setValueProperty ),
				$"Unexpected value in set value property '{setValueProperty}'." );
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Property Label.mfb", users );

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
		/// Verify label for an unknown property using configuration rule.
		/// </summary>
		[Test]
		[Category( "Metadata Configuration" )]
		[TestCase(
			"Document",
			"Multi-File Document",
			"Class",
			"Drawing")]
		public void LabelForAnUnknownPropertyOnExistingObject(
			string objectType,
			string template,
			string triggeringProperty,
			string triggeringValue)
		{
			

			//string configFileName = $"{classID}\\LabelForAnUnknownPropertyOnExistingObject.json";

			string configFileName = $"{classID}\\LabelForAnUnknownPropertyOnExistingObject_GUID.json";

			string configuredLabel = "Time $$ Plus $$ Test";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Start creating a document.
			MetadataCardPopout newMDCard = homePage.TopPane.CreateNewObjectFromTemplate( objectType, template );

			// Set class property. This will trigger a rule that adds multiple properties to the metadata card
			// and sets their values. The property count should be in total 11, after setting the class.
			newMDCard.Properties.SetPropertyValue( triggeringProperty, triggeringValue, 11 );

			System.Threading.Thread.Sleep(2000);

			string currentTime = DateTime.Now.ToShortTimeString();

			// Assert all set property values.
			// Check the Name and title property value.
			this.AssertSetValueProperty( "Default Text Sample", "Name or title", newMDCard.Properties );

			// Check the customer property values.
			Assert.AreEqual( new List<string> { "A&A Consulting (AEC)", "Warwick Systems & Technology" }, newMDCard.Properties.GetMultiSelectLookupPropertyValues( "Customer" ),
				"Set property value did not match the expected customer values." );

			// Check the drawing type property values.
			Assert.AreEqual( new List<string> { "Electrical", "Architectural" }, newMDCard.Properties.GetMultiSelectLookupPropertyValues( "Drawing type" ),
				"Set property value did not match the expected drawing type values." );

			string dateValue = DateTime.Now.AddDays( 5 ).ToShortDateString();

			// Assert that document date is set to 5 days in advance.
			this.AssertSetValueProperty( dateValue, "Document date", newMDCard.Properties );

			// Assert that description is set to configured text.
			this.AssertSetValueProperty( "Default Text Sample\r\nline 2 \r\nline 3", "Description", newMDCard.Properties );

			// Assert that defaultTime property is set to current time.
			//this.AssertSetValueProperty( currentTime, "DefaultTime", newMDCard.Properties );

			this.AssertSetValueProperty( "NaN:NaN:NaN PM", "DefaultTime", newMDCard.Properties );

			// Assert that the label is changed by the rule.
			Assert.True( newMDCard.Properties.IsPropertyInMetadataCard( configuredLabel ) );

			//NaN:NaN:NaN PM 12:00:00 AM

			// Assert that label property is set to configured static value.
			//this.AssertSetValueProperty( "12:30:00 PM", configuredLabel, newMDCard.Properties );

			this.AssertSetValueProperty( "NaN:NaN:NaN PM", configuredLabel, newMDCard.Properties );

			newMDCard.DiscardChanges();
		}
		/// <summary>
		/// Trigger configuration rule to change a property's label values with JavaScript.
		/// </summary>
		[Test]
		[Category( "Metadata Configuration" )]
		[TestCase(
			"DAT Sports & Entertainment",
			"Customer",
			"Department",
			"Sales",
			"Customer name" )]
		public void JavaScriptPropertyLabelConfigurationOnExistingObject(
			string objectName,
			string objectType,
			string triggeringProperty,
			string triggeringValue,
			string configuredProperty
			 )
		{
			string configuredLabel = "{script}alert( &#39;Hello, I am a javascript bug!&#39; ){/script}";

			string configFileName = $"{classID}\\JavaScriptPropertyLabelConfigurationOnExistingObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			string propertyLabelDeviationErrorMsg = "Default property label is not displayed when rule is not active.";

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Assert that the property contains Default label before triggering the configuration rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredProperty ), propertyLabelDeviationErrorMsg );

			// Add triggering property and Trigger the rule by setting property value.
			mdCard.Properties.AddProperty( triggeringProperty );
			mdCard.Properties.SetPropertyValue( triggeringProperty, triggeringValue, 12 );

			System.Threading.Thread.Sleep( 2000 );

			// Assert that the label is changed by the rule.
			// Note: Label characters '<>' converted to '{}' and characters '' converted to &#39;
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredLabel ), "Expected javascript property label not present." );

			// Set trigger property to some other value so that rule is not triggered.
			mdCard.Properties.SetPropertyValue( triggeringProperty, "Production", 12 );

			System.Threading.Thread.Sleep(2000);

			// Assert property label changed back to default value as rule is not true anymore.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredProperty ), propertyLabelDeviationErrorMsg );

			mdCard.DiscardChanges();
		}
		/// <summary>
		/// Trigger configuration rule to change a property's label values with link.
		/// </summary>
		[Test]
		[Category( "Metadata Configuration" )]
		[TestCase(
			"DAT Sports & Entertainment",
			"Customer",
			"Department",
			"Sales",
			"Customer name")]
		public void LinkPropertyLabelConfigurationOnExistingObject(
			string objectName,
			string objectType,
			string triggeringProperty,
			string triggeringValue,
			string configuredProperty )
		{
			string configuredLabel = "<a href='about:blank' target='_blank' class='mf-executable-url' data-target='http://www.m-files.com/en'>http M-Files web link</a>";

			string configFileName = $"{classID}\\LinkPropertyLabelConfigurationOnExistingObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Assert that the property contains normal label before triggering the configuration rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredProperty ) );

			System.Threading.Thread.Sleep(7000);

			// Add triggering property and Trigger the rule by setting property value.
			mdCard.Properties.AddProperty( triggeringProperty );
			mdCard.Properties.SetPropertyValue( triggeringProperty, triggeringValue, 12 );

			// Assert that the label is changed by the rule.
			// Note: Label will be a plain text and label characters '<>' converted to '{}' and characters '' converted to &#39;
			Assert.Contains( configuredLabel , mdCard.Properties.PropertyNames, "Configured property link is not present in properties list." );

			mdCard.DiscardChanges();
		}

		/// <summary>
		/// Test property Label using different data types in property condition.
		/// </summary>
		[Test]
		[Category( "Metadata Configuration" )]
		[TestCase(
			"Project Schedule.pdf",
			"Document",
			"Accepted",
			"No",
			"Description",
			"Description Labeled" )]
		public void LabelUsingDifferentDataTypesOnExistingObject(
			string objectName,
			string objectType,
			string triggeringProperty,
			string triggeringValue,
			string configuredProperty,
			string configuredLabel )
		{
			string configFileName = $"{classID}\\LabelUsingDifferentDataTypesOnExistingObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Add triggering property and Trigger the rule by setting property value.
			mdCard.Properties.AddProperty( triggeringProperty );
			mdCard.Properties.SetPropertyValue( triggeringProperty, triggeringValue, 10 );

			// Assert that the label is changed by the rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredLabel ) );

			mdCard.DiscardChanges();

			homePage.SearchPane.QuickSearch( "Annual General Meeting Agenda.doc" );

			mdCard = listing.SelectObject( "Annual General Meeting Agenda.doc" );

			// Assert that the label is changed by the rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredLabel ) );
		}

		/// <summary>
		/// Test property labeling properties with combined rule (Object Type, Class ID, Property ID, aliases, GUIDs).
		/// </summary>
		[Test]
		[Category( "Metadata Configuration" )]
		[TestCase(
			"Label Description.xlsx",
			"Document",
			"Description Labeled" )]
		public void LabelPropertiesWithCombinedRuleOnExistingObject(
			string objectName,
			string objectType,
			string configuredLabel )
		{
			string configFileName = $"{classID}\\LabelPropertiesWithCombinedRuleOnExistingObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Assert that the label is changed by the rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredLabel ) );
		}

		/// <summary>
		/// Test property labeling properties with different operators.
		/// </summary>
		[Test]
		[Category( "Metadata Configuration" )]
		public void LabelPropertiesWithDifferentOperatorsOnExistingObject()
		{
			string configFileName = $"{classID}\\LabelPropertiesWithDifferentOperatorsOnExistingObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			// Start test: Make a search and select an object.
			HomePage homePage = this.browserManager.StartTestAtHomePage();
			string objectName = "Project Agreement - CBH International (10/2018).docx";
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, "Document" );
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Add property for which label configuration is applied.
			string configuredProperty = "Additional classes";
			mdCard.Properties.AddProperty( configuredProperty );

			// Assert property label before rule is applied.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredProperty ) );

			// Setting certain values to this property will trigger a rule.
			string triggeringPropertyCustomer = "Customer";
			string triggeringPropertyDepartment = "Department";
			//string triggeringPropertyAgreementType = "Agreement type";

			// Set the triggering property value and wait for it to trigger autofill dialog.
			// Note: As per rule customer property = A&A Consulting (AEC),CBH International and City of Chicago (Planning and Development)
			// department = Administration  && Agreement type != Non-disclosure Agreement.
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( triggeringPropertyCustomer, "A&A Consulting (AEC)", 0);
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( triggeringPropertyCustomer, "CBH International", 1 );
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( triggeringPropertyCustomer, "City of Chicago (Planning and Development)", 2 );

			mdCard.Properties.SetPropertyValue( triggeringPropertyDepartment, "Administration", 14 );

			// Note: Searched object type already had Agreement type != Non-disclosure Agreement so value is not set.

			// Store configured label value.
			string configuredLabel = "More Classes";

			// Assert that the label is changed by the rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredLabel ) );

			mdCard.DiscardChanges();
		}

		/// <summary>
		/// Test property labeling properties with object/class id aliases and GUIDs.
		/// </summary>
		[Test]
		[Category( "Metadata Configuration" )]
		public void LabelPropertiesWithAliasesAndGUIDsOnExistingObject()
		{
			string configFileName = $"{classID}\\LabelPropertiesWithAliasesAndGUIDsOnExistingObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			// Start test: Make a search and select an object.
			HomePage homePage = this.browserManager.StartTestAtHomePage();
			string objectName = "Fortney Nolte Associates";
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, "Customer" );
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Setting certain values to these property will trigger a rule.
			string triggeringPropertyCountry = "Country";
			string triggeringPropertyDepartment = "Department";

			// Add all the triggering country property value to a List.
			List<string> triggeringCountryValue = new List<string>{ "Canada", "China", "France" };

			// Declare the Department triggering property value.
			string triggeringDepartmentValue = "Administration";

			// Add trigger property to the metadata card.
			// Note: Country property was already present in metadata card so not added.
			mdCard.Properties.AddProperty( triggeringPropertyDepartment );

			// Add configured property to the metadata card.
			// Note: City property was already present in metadata card so not added.
			string configuredPropertyCity = "City";
			string configuredPropertyDescription = "Description";
			mdCard.Properties.AddProperty( configuredPropertyDescription );

			// Configured Labels.
			string configuredCityLabel = "Client City";
			string configuredDescriptionLabel = "Description 123";

			//Rule: Country = "Canada", "China" and "France" && Department = "Administration".
			// Label should change for city and Description properties.

			// Set the triggering property value.
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( triggeringPropertyCountry, triggeringCountryValue[0], 0 );
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( triggeringPropertyCountry, triggeringCountryValue[ 1 ], 1 );
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( triggeringPropertyCountry, triggeringCountryValue[ 2 ], 2 );
			mdCard.Properties.SetPropertyValue( triggeringPropertyDepartment, triggeringDepartmentValue, 12 );

			// Assert that the label is changed by the rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredCityLabel ) );
			
			// Assert that the label is changed by the rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredDescriptionLabel ) );

			// Change any triggering value. Rule should be inactive.
			mdCard.Properties.SetPropertyValue( triggeringPropertyDepartment, "Marketing", 12 );

			// Assert that the label is changed by the rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredPropertyCity ) );

			// Assert that the label is changed by the rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredPropertyDescription ) );

			// Add the remove triggering property value again. Rule should be active.
			mdCard.Properties.SetPropertyValue( triggeringPropertyDepartment, triggeringDepartmentValue, 12 );

			// Assert that the label is changed by the rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredCityLabel ) );

			// Assert that the label is changed by the rule.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredDescriptionLabel ) );

			// Discard changes.
			mdCard.DiscardChanges();
		}

		/// <summary>
		/// Test property nested labeling rules for object/class ID.
		/// </summary>
		[Test]
		[Category( "Metadata Configuration" )]
		public void PropertyNestedLabelingOnNewObject()
		{
			string configFileName = $"{classID}\\PropertyNestedLabelingOnNewObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			// Start test: Start creating an object of type Document collection.
			HomePage homePage = this.browserManager.StartTestAtHomePage();
			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( "Document collection" );

			
			// All configured property in the metadata card.
			// Note: City property was already present in metadata card so not added.
			string configuredPropertyName = "Name or title";
			string configuredPropertyDescription = "Description";
			string configuredPropertyET = "Effective through";
			string configuredPropertyKeywords = "Keywords";
			string configuredPropertyAT = "Agreement title";

			// Configured Labels.
			string configuredNameLabel = "Name or title (level 1)";
			string configuredDescriptionLabel = "Description (level 2_1)";
			string configuredETLabel = "Effective through (level 3_1)";
			string configuredKeywordsLabel1 = "Keywords (level 2_2)";
			string configuredATLabel = "Agreement title (level 3_2)";
			string configuredKeywordsLabel2 = "Keywords (level 4_2)";

			// Nested 1 rule will triggered as object type is Document collection.
			//Assert.True( newObjMDCard.Properties.IsPropertyInMetadataCard( configuredNameLabel ) );

			// Setting certain values to this property will trigger a rule.
			string triggeringPropertyClass = "Class"; // Memo, Agenda
			string triggeringPropertyCustomer = "Customer"; // A&A Consulting (AEC),CBH International
			string triggeringPropertyAT = "Agreement type"; // != Non-disclosure Agreement
			string triggeringPropertyAccepted = "Accepted"; // Yes

			// Set the triggering property value.
			// Nested 2_1 rule condition: Class = Memo.
			newObjMDCard.Properties.SetPropertyValue( triggeringPropertyClass, "Memo", 8);

			// Assert that the label is changed by the rule.
			Assert.True( newObjMDCard.Properties.IsPropertyInMetadataCard( configuredDescriptionLabel ) );
			Assert.True( newObjMDCard.Properties.IsPropertyInMetadataCard( configuredNameLabel ) );

			// Add trigger property to the metadata card.
			newObjMDCard.Properties.AddProperty( triggeringPropertyCustomer );

			// Add configured missing property (Effective through).
			newObjMDCard.Properties.AddProperty( configuredPropertyET );
			
			// Set the triggering property value.
			// Nested 3_1 rule condition: Class = Memo.
			newObjMDCard.Properties.SetMultiSelectLookupPropertyValueByIndex( triggeringPropertyCustomer, "A&A Consulting (AEC)", 0 );
			newObjMDCard.Properties.SetMultiSelectLookupPropertyValueByIndex( triggeringPropertyCustomer, "CBH International", 1 );

			System.Threading.Thread.Sleep( 2000 );

			// Assert that the label is changed by the rule.
			Assert.True( newObjMDCard.Properties.IsPropertyInMetadataCard( configuredETLabel ) );

			// Set the triggering property value.
			// Nested 2_2 rule condition: Class = Agenda.
			newObjMDCard.Properties.SetPropertyValue( triggeringPropertyClass, "Agenda", 9 );

			System.Threading.Thread.Sleep( 2000 );

			// Assert that the label is changed by the rule.
			Assert.True( newObjMDCard.Properties.IsPropertyInMetadataCard( configuredKeywordsLabel1 ) );

			// Add configured missing property (Agreement title).
			newObjMDCard.Properties.AddProperty( configuredPropertyAT );

			// Add triggering property then set triggering property value.
			// Nested 3_2 rule condition: Agreement type!=Non-disclosure Agreement.
			newObjMDCard.Properties.AddProperty( triggeringPropertyAT );
			newObjMDCard.Properties.SetPropertyValue( triggeringPropertyAT, "Project Agreement", 11 );

			System.Threading.Thread.Sleep( 2000 );

			// Assert that the label is changed by the rule.
			Assert.True( newObjMDCard.Properties.IsPropertyInMetadataCard( configuredATLabel ) );

			// Add triggering property then set triggering property value.
			// Nested 4_2 rule condition: Accepted=Yes.
			newObjMDCard.Properties.AddProperty( triggeringPropertyAccepted );
			newObjMDCard.Properties.SetPropertyValue( triggeringPropertyAccepted, "Yes", 12 );

			System.Threading.Thread.Sleep( 2000 );

			// Assert that the label is changed by the rule.
			Assert.True( newObjMDCard.Properties.IsPropertyInMetadataCard( configuredKeywordsLabel2 ) );

			// Discard changes.
			newObjMDCard.DiscardChanges();
		}

		/// <summary>
		/// Replace property default label twice.
		/// </summary>
		[Test]
		[Category( "Metadata Configuration" )]
		public void ReplacePropertyDefaultLabelTwiceOnExistingObject()
		{
			string configFileName = $"{classID}\\ReplacePropertyDefaultLabelTwiceOnExistingObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			// Start test: Make a search and select an object.
			HomePage homePage = this.browserManager.StartTestAtHomePage();
			string objectName = "Office Design";
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, "Project" );
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Selecting project object will trigger first rule provided class is not equal to Customer Project.
			// Store configured label value.
			string configuredLabelRule1 = "Custom Eventdate";
			string configuredLabelRule2 = "Custom Eventdate (again)";

			// Set class to Internal project and trigger rule 1.
			mdCard.Properties.SetPropertyValue( "Class", "Internal Project", 7 );

			System.Threading.Thread.Sleep( 2000 );

			// Assert that the label is changed by the rule1.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredLabelRule1 ) );

			// Set class to Customer Project and trigger rule 2.
			mdCard.Properties.SetPropertyValue( "Class", "Customer Project", 7 );

			System.Threading.Thread.Sleep( 2000 );

			// Assert that the label is changed by the rule1.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredLabelRule2 ) );

			mdCard.DiscardChanges();
		}

		/// <summary>
		/// Replace more than two property default labels.
		/// </summary>
		[Test]
		[Category( "Metadata Configuration" )]
		public void ReplaceMoreThanTwoPropertyDefaultLabelOnExistingObject()
		{
			string configFileName = $"{classID}\\ReplaceMoreThanTwoPropertyDefaultLabelOnExistingObject.json";

			EnvironmentSetupHelper.SetupMetadataCardConfiguration( mfContext, configFileName );

			// Start test: Make a search and select an object.
			HomePage homePage = this.browserManager.StartTestAtHomePage();
			string objectName = "Test";
			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, "Assignment" );
			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Selecting project object will trigger first rule provided class is not equal to Customer Project.
			// Store configured label value.
			string configuredLabel1 = "First Deadline";
			string configuredLabel2 = "Absolute Deadline";
			string configuredLabel3 = "Deadline Description";

			// Setting certain values to this property will trigger a rule.
			string triggeringPropertyEmployee = "Assigned to";

			// Set trigger value.
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( triggeringPropertyEmployee, "Alex Kramer", 0 );
			mdCard.Properties.SetMultiSelectLookupPropertyValueByIndex( triggeringPropertyEmployee, "Andy Nash", 1 );

			System.Threading.Thread.Sleep(2000);

			// Assert that the label is changed by the rule1.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredLabel1 ) );

			// Assert that the label is changed by the rule1.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredLabel2 ) );

			// Assert that the label is changed by the rule1.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredLabel3 ) );

			mdCard.DiscardChanges();
		}
	}
}
