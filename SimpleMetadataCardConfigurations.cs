using System.Collections.Generic;
using Motive.MFiles.API.Framework;
using Motive.MFiles.vNextUI.PageObjects;
using Motive.MFiles.vNextUI.PageObjects.MetadataCard;
using Motive.MFiles.vNextUI.Utilities;
using Motive.MFiles.vNextUI.Utilities.GeneralHelpers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Motive.MFiles.vNextUI.Tests
{
	[Order( -9 )]
	[Parallelizable( ParallelScope.Self )]
	class SimpleMetadataCardConfigurations
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

		public SimpleMetadataCardConfigurations()
		{
			this.classID = "SimpleMetadataCardConfigurations";
		}

		[OneTimeSetUp]
		public void SetupTestClass()
		{
			// Initialize configurations for the test class based on test context parameters.
			this.configuration = new TestClassConfiguration( this.classID, TestContext.Parameters );

			// Define users required by this test class.
			UserProperties[] users = EnvironmentSetupHelper.GetBasicTestUsers();

			// TODO: Some environment details should probably come from configuration. For example the backend.
			this.mfContext = EnvironmentSetupHelper.SetupEnvironment( EnvironmentHelper.VaultBackend.Firebird, "Metadata Card Configuration.mfb", users );

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
		/// Click a property and view the configured description.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"South Elevation.dwg",
			"Document",
			"Revision number",
			"Please, don't use characters, such as ÅÄÖ åäö !@#$%^&*()_+{}|:" )]
		public void PropertyDescriptionConfigurationOnExistingObject(
			string objectName,
			string objectType,
			string property,
			string expectedPropertyDescription )
		{

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Click the property to display the configured description.
			string actualDescription =
				mdCard.ConfigurationOperations.ClickPropertyAndGetPropertyDescription( property );

			// Assert the property description.
			Assert.AreEqual( expectedPropertyDescription, actualDescription );

		}

		/// <summary>
		/// Trigger configuration rule to change a property's label.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Annual General Meeting Agenda.doc",
			"Document",
			"Meeting type",
			"Marketing meeting",
			"Description",
			"Marketing text" )]
		public void PropertyLabelConfigurationTriggeredOnExistingObject(
			string objectName,
			string objectType,
			string triggeringProperty,
			string triggeringValue,
			string configuredProperty,
			string configuredLabel )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			MetadataCardPopout popoutMDCard = mdCard.PopoutMetadataCard();

			// Assert that the property contains normal label before triggering the configuration rule.
			Assert.True( popoutMDCard.Properties.IsPropertyInMetadataCard( configuredProperty ) );

			// Trigger the rule by setting property value.
			popoutMDCard.Properties.SetPropertyValue( triggeringProperty, triggeringValue );

			// Assert that the label is changed by the rule.
			Assert.True( popoutMDCard.Properties.IsPropertyInMetadataCard( configuredLabel ) );

			popoutMDCard.SaveAndDiscardOperations.Save();
		}

		/// <summary>
		/// Trigger configuration rule to set value to a property.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Project tasks - CRM development.xlsx",
			"Document",
			"Reply to",
			"Project Schedule",
			"Keywords",
			"The project is CRM Application Development." )]
		public void SetValueConfigurationTriggeredOnExistingObject(
			string objectName,
			string objectType,
			string triggeringProperty,
			string triggeringValue,
			string configuredProperty,
			string configuredValue )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Trigger the configuration rule by setting value to a property.
			mdCard.Properties.AddPropertyAndSetValue( triggeringProperty, triggeringValue );

			// Assert that the property value is set as configured.
			Assert.AreEqual( configuredValue, mdCard.Properties.GetPropertyValue( configuredProperty ) );

			mdCard.SaveAndDiscardOperations.Save();
		}

		/// <summary>
		/// Trigger configuration rule to add a property to metadata card.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Tennessee Land Surveyors (Nashville)",
			"Customer",
			"Country",
			"Canada",
			"Description" )]
		public void IsAdditionalPropertyConfigurationTriggeredOnExistingObject(
			string objectName,
			string objectType,
			string triggeringProperty,
			string triggeringValue,
			string configuredProperty )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			MetadataCardPopout popoutMDCard = mdCard.PopoutMetadataCard();

			// Trigger the configuration rule by setting value to a property.
			popoutMDCard.Properties.SetPropertyValue( triggeringProperty, triggeringValue );

			// Assert that the configured additional property appears.
			Assert.True( popoutMDCard.Properties.IsPropertyInMetadataCard( configuredProperty ) );

			mdCard = popoutMDCard.SaveAndDiscardOperations.Save();

			// Assert also that the additional property exists on right pane metadata card.
			Assert.True( mdCard.Properties.IsPropertyInMetadataCard( configuredProperty ) );
		}

		/// <summary>
		/// Trigger configuration rule to set a property as a required property.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"CRM Application Development",
			"Project",
			"Project manager",
			"Mike Taylor",
			"Contact person" )]
		public void IsRequiredPropertyConfigurationTriggeredOnExistingObject(
			string objectName,
			string objectType,
			string triggeringProperty,
			string triggeringValue,
			string configuredProperty )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Trigger the configuration rule by setting value to a property.
			mdCard.Properties.SetPropertyValue( triggeringProperty, triggeringValue );

			// Assert that the property is set as a required property.
			Assert.True( mdCard.Properties.IsPropertyRequired( configuredProperty ),
				$"Configured property '{configuredProperty}' does not appear as a required property." );

			mdCard.SaveAndDiscardOperations.Save();

			// Assert that the property is set as a required property, also after saving the metadata card.
			Assert.True( mdCard.Properties.IsPropertyRequired( configuredProperty ),
				$"Configured property '{configuredProperty}' does not appear as a required property." );
		}

		/// <summary>
		/// Trigger configuration rule to set a property as read-only.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Sales Invoice 239 - City of Chicago (Planning and Development).xls",
			"Document",
			"Project",
			"Office Design",
			"Document date" )]
		public void IsReadOnlyPropertyConfigurationTriggeredOnExistingObject(
			string objectName,
			string objectType,
			string triggeringProperty,
			string triggeringValue,
			string configuredProperty )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			MetadataCardPopout popoutMDCard = mdCard.PopoutMetadataCard();

			// Trigger the configuration rule by setting value to a property.
			popoutMDCard.Properties.SetPropertyValue( triggeringProperty, triggeringValue );

			// Assert that the configured property cannot be modified.
			Assert.False( popoutMDCard.Properties.CanPropertyEditModeBeActivated( configuredProperty ) );

			mdCard = popoutMDCard.SaveAndDiscardOperations.Save();

			// Assert also on right pane metadata card that the configured property cannot be modified.
			Assert.False( mdCard.Properties.CanPropertyEditModeBeActivated( configuredProperty ) );
		}

		/// <summary>
		/// Open metadata card of an object and verify that a property is hidden by configuration rule.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Andy Nash",
			"Employee",
			"M-Files user" )]
		public void IsHiddenPropertyConfigurationOnExistingObject(
			string objectName,
			string objectType,
			string configuredProperty )
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Assert that hidden property should not be visible in the metadata card.
			Assert.True( mdCard.ConfigurationOperations.IsPropertyHidden( configuredProperty ) );

			MetadataCardPopout popoutMDCard = mdCard.PopoutMetadataCard();

			// Also assert in popped out metadata card that hidden property 
			// should not be visible in the metadata card.
			Assert.True( popoutMDCard.ConfigurationOperations.IsPropertyHidden( configuredProperty ) );

			popoutMDCard.CloseButtonClick();
		}

		/// <summary>
		/// Open metadata card of an object and verify that a property has a configured tooltip.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Lily.jpg",
			"Document",
			"Description",
			"Describing ÅÄÖåäö!"
			)]
		public void PropertyTooltipConfigurationOnExistingObject(
			string objectName,
			string objectType,
			string tooltipProperty,
			string expectedTooltip
			)
		{
			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Assert that the property has tooltip.
			Assert.AreEqual( expectedTooltip, mdCard.ConfigurationOperations.GetPropertyTooltip( tooltipProperty ) );
		}

		/// <summary>
		/// Open metadata card of an object and verify that configured property groups are 
		/// displayed in expected order.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Robert Brown",
			"Contact person",
			";Name information;Details and contact information;Object information" )]
		public void PropertyGroupsConfigurationPriorityOrderOnExistingObject(
			string objectName,
			string objectType,
			string propertyGroups )
		{

			List<string> expectedPropertyGroups =
				StringSplitHelper.ParseStringToStringList( propertyGroups, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			MetadataCardPopout popoutMDCard = mdCard.PopoutMetadataCard();

			List<string> actualPropertyGroups = popoutMDCard.ConfigurationOperations.PropertyGroups;

			// Assert that the property groups are in expected order.
			Assert.AreEqual( expectedPropertyGroups, actualPropertyGroups );

			popoutMDCard.CloseButtonClick();
		}

		/// <summary>
		/// Start creating a new object and verify that configured property groups are displayed
		/// in expected order.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Contact person",
			";Name information;Details and contact information;Object information" )]
		public void PropertyGroupsConfigurationPriorityOrderOnNewObject(
			string objectType,
			string propertyGroups )
		{
			List<string> expectedPropertyGroups =
				StringSplitHelper.ParseStringToStringList( propertyGroups, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			// Make an empty search. Basically this could also be done at home page but let's start 
			// creating new object in search view.
			ListView listing = homePage.SearchPane.FilteredQuickSearch( "", objectType );

			MetadataCardPopout newObjMDCard = homePage.TopPane.CreateNewObject( objectType );

			List<string> actualPropertyGroupsNewObj = newObjMDCard.ConfigurationOperations.PropertyGroups;

			// Assert that the property groups are in expected order.
			Assert.AreEqual( expectedPropertyGroups, actualPropertyGroupsNewObj );

			// Not creating the new object.
			newObjMDCard.DiscardChanges();

		}

		/// <summary>
		/// Open metadata card of an object and verify that a property group is expanded by default.
		/// Then verify the properties inside the group. Finally, collapse the group and verify that the
		/// properties are hidden.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Tina Nolte",
			"Contact person",
			"Details and contact information",
			"Job title;Telephone number;E-mail address" )]
		public void PropertyGroupsConfigurationExpandedGroupOnExistingObject(
			string objectName,
			string objectType,
			string propertyGroup,
			string groupProperties )
		{
			List<string> expectedGroupProperties =
				StringSplitHelper.ParseStringToStringList( groupProperties, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			MetadataCardPopout popoutMDCard = mdCard.PopoutMetadataCard();

			// Assert that the property group is expanded by default.
			Assert.AreEqual( MetadataCardConfigurationOperations.PropertyGroupStatus.Expanded,
				popoutMDCard.ConfigurationOperations.GetPropertyGroupStatus( propertyGroup ) );

			List<string> actualGroupProperties = popoutMDCard.ConfigurationOperations.GetPropertiesInGroup( propertyGroup );

			// Assert that the expected properties are listed under the property group.
			Assert.AreEqual( expectedGroupProperties, actualGroupProperties );

			popoutMDCard.ConfigurationOperations.CollapsePropertyGroup( propertyGroup );

			// Assert that the property group was collapsed.
			Assert.AreEqual( MetadataCardConfigurationOperations.PropertyGroupStatus.Collapsed,
				popoutMDCard.ConfigurationOperations.GetPropertyGroupStatus( propertyGroup ) );

			// Assert that the properties were hidden when the group was collapsed.
			foreach( string property in expectedGroupProperties )
			{
				Assert.True( popoutMDCard.ConfigurationOperations.IsPropertyHidden( property ) );
			}

			popoutMDCard.CloseButtonClick();
		}

		/// <summary>
		/// Open metadata card of an object and verify that a property group is collapsed by default.
		/// Then verify that properties inside the group are hidden. Finally, expand the group and verify 
		/// that the properties inside the group are displayed.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Ben Griffith",
			"Contact person",
			"Object information",
			"Owner (Customer)" )]
		public void PropertyGroupsConfigurationCollapsedGroupOnExistingObject(
			string objectName,
			string objectType,
			string propertyGroup,
			string groupProperties )
		{
			List<string> expectedGroupProperties =
				StringSplitHelper.ParseStringToStringList( groupProperties, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			// Assert that the property group is collapsed by default.
			Assert.AreEqual( MetadataCardConfigurationOperations.PropertyGroupStatus.Collapsed,
				mdCard.ConfigurationOperations.GetPropertyGroupStatus( propertyGroup ) );

			// Assert that properties in the group are hidden while the group is collapsed.
			foreach( string property in expectedGroupProperties )
			{
				Assert.True( mdCard.ConfigurationOperations.IsPropertyHidden( property ) );
			}

			mdCard.ConfigurationOperations.ExpandPropertyGroup( propertyGroup );

			// Assert that the property group was expanded.
			Assert.AreEqual( MetadataCardConfigurationOperations.PropertyGroupStatus.Expanded,
				mdCard.ConfigurationOperations.GetPropertyGroupStatus( propertyGroup ) );

			List<string> actualGroupProperties = mdCard.ConfigurationOperations.GetPropertiesInGroup( propertyGroup );

			// Assert that the properties are displayed in the expanded group.
			Assert.AreEqual( expectedGroupProperties, actualGroupProperties );
		}

		/// <summary>
		/// Open metadata card of an object and verify that a property group cannot be collapsed.
		/// Then verify that it contains expected properties.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Don Ortiz",
			"Contact person",
			"Name information",
			"Full name;First name;Last name" )]
		public void PropertyGroupsConfigurationNonCollapsibleGroupOnExistingObject(
			string objectName,
			string objectType,
			string propertyGroup,
			string groupProperties )
		{

			List<string> expectedGroupProperties =
				StringSplitHelper.ParseStringToStringList( groupProperties, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			MetadataCardPopout mdCardPopout = mdCard.PopoutMetadataCard();

			// Assert that the property group is not collapsible
			Assert.AreEqual( MetadataCardConfigurationOperations.PropertyGroupStatus.NotCollapsible,
				 mdCardPopout.ConfigurationOperations.GetPropertyGroupStatus( propertyGroup ) );

			List<string> actualGroupProperties = mdCardPopout.ConfigurationOperations.GetPropertiesInGroup( propertyGroup );

			// Assert that expected properties are displayed in the property group.
			Assert.AreEqual( expectedGroupProperties, actualGroupProperties );

			mdCardPopout.CloseButtonClick();

		}

		/// <summary>
		/// Open metadata card of an object and verify that property displays values as checkboxes.
		/// The add some checkbox values and save. Finally, remove/unselect the values and save again.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Invitation to Project Meeting 1/2004.doc",
			"Document",
			"Meeting type",
			"Project meeting",
			"General meeting;Staff meeting",
			"General meeting;Project meeting;Staff meeting" )]
		public void AddAndRemoveValuesByCheckboxConfigurationOnExistingObject(
			string objectName,
			string objectType,
			string checkboxProperty,
			string existingValues,
			string addAndRemoveValues,
			string combinedValues
			)
		{
			// Selected checkbox property values at start of the test.
			List<string> existingPropValues =
				StringSplitHelper.ParseStringToStringList( existingValues, ';' );

			// Add these property values by selecting their checkboxes.
			List<string> addAndRemovePropValues =
				StringSplitHelper.ParseStringToStringList( addAndRemoveValues, ';' );

			// Existing and added property values in expected order.
			List<string> combinedPropValues =
				StringSplitHelper.ParseStringToStringList( combinedValues, ';' );

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			MetadataCardPopout popoutMDCard = mdCard.PopoutMetadataCard();

			// Assert that existing values in the property are displayed as selected checkboxes.
			Assert.AreEqual( existingPropValues, popoutMDCard.ConfigurationOperations.GetSelectedCheckboxValues( checkboxProperty ) );

			// Select more values.
			foreach( string addValue in addAndRemovePropValues )
			{
				popoutMDCard.ConfigurationOperations.SelectCheckboxPropertyValue( checkboxProperty, addValue );
			}

			// Assert that all existing and added values are selected.
			Assert.AreEqual( combinedPropValues, popoutMDCard.ConfigurationOperations.GetSelectedCheckboxValues( checkboxProperty ) );

			mdCard = popoutMDCard.SaveAndDiscardOperations.Save();

			// Assert that expected values are still selected after saving.
			Assert.AreEqual( combinedPropValues, mdCard.ConfigurationOperations.GetSelectedCheckboxValues( checkboxProperty ) );

			// Remove the selection of the added values.
			foreach( string removeValue in addAndRemovePropValues )
			{
				mdCard.ConfigurationOperations.UnselectCheckboxPropertyValue( checkboxProperty, removeValue );
			}

			// Assert that the selection was removed from the values.
			Assert.AreEqual( existingPropValues, mdCard.ConfigurationOperations.GetSelectedCheckboxValues( checkboxProperty ) );

			mdCard = mdCard.SaveAndDiscardOperations.Save();

			// Assert that the values are still unselected after saving.
			Assert.AreEqual( existingPropValues, mdCard.ConfigurationOperations.GetSelectedCheckboxValues( checkboxProperty ) );
		}

		/// <summary>
		/// Open metadata card of an object and verify property values are displayed as radio buttons. Then select
		/// a radio button value and save.
		/// </summary>
		[Test]
		[Category( "Smoke" )]
		[TestCase(
			"Sales Strategy Development",
			"Project",
			"Project manager",
			"Tina Smith",
			"Bill Richards" )]
		public void RadioButtonConfigurationOnExistingObject(
			string objectName,
			string objectType,
			string radioButtonProperty,
			string existingValue,
			string changeToValue )
		{

			HomePage homePage = this.browserManager.StartTestAtHomePage();

			ListView listing = homePage.SearchPane.FilteredQuickSearch( objectName, objectType );

			MetadataCardRightPane mdCard = listing.SelectObject( objectName );

			MetadataCardPopout popoutMDCard = mdCard.PopoutMetadataCard();

			// Assert that a value is displayed as selected in a radio button.
			Assert.AreEqual( existingValue, popoutMDCard.ConfigurationOperations.GetSelectedRadioButtonPropertyValue( radioButtonProperty ) );

			// Select some other value to the property by using radio button.
			popoutMDCard.ConfigurationOperations.SelectRadioButtonPropertyValue( radioButtonProperty, changeToValue );

			// Assert that the value was changed.
			Assert.AreEqual( changeToValue, popoutMDCard.ConfigurationOperations.GetSelectedRadioButtonPropertyValue( radioButtonProperty ) );

			mdCard = popoutMDCard.SaveAndDiscardOperations.Save();

			// Assert that the radio button value is selected after saving the changes.
			Assert.AreEqual( changeToValue, mdCard.ConfigurationOperations.GetSelectedRadioButtonPropertyValue( radioButtonProperty ) );
		}
	}
}
