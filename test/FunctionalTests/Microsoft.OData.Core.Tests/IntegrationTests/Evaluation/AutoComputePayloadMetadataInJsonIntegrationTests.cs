﻿//---------------------------------------------------------------------
// <copyright file="AutoComputePayloadMetadataInJsonIntegrationTests.cs" company="Microsoft">
//      Copyright (C) Microsoft Corporation. All rights reserved. See License.txt in the project root for license information.
// </copyright>
//---------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.OData.UriParser;
using Microsoft.OData.Edm;
using Microsoft.Test.OData.DependencyInjection;
using Xunit;

namespace Microsoft.OData.Tests.IntegrationTests.Evaluation
{
    public class AutoComputePayloadMetadataInJsonIntegrationTests
    {
        private readonly ODataResource entryWithPayloadMetadata = new ODataResource
        {
            Properties = new[] {
                    new ODataProperty { Name = "ID", Value = 123 },
                    new ODataProperty
                    {
                        Name = "StreamProp1",
                        Value = new ODataStreamReferenceValue
                        {
                            ContentType = "image/jpeg",
                            EditLink = new Uri("http://example.com/stream/edit"),
                            ReadLink = new Uri("http://example.com/stream/read"),
                            ETag = "stream etag"
                        }
                    }},
            EditLink = new Uri("http://example.com/edit"),
            ReadLink = new Uri("http://example.com/read"),
            Id = new Uri("http://example.com/id"),
            ETag = "etag",
            MediaResource = new ODataStreamReferenceValue
            {
                ContentType = "image/png",
                EditLink = new Uri("http://example.com/mr/edit"),
                ReadLink = new Uri("http://example.com/mr/read"),
                ETag = "mr etag"
            },
        };

        private readonly ODataNestedResourceInfo navLinkWithPayloadMetadata = new ODataNestedResourceInfo
        {
            AssociationLinkUrl = new Uri("http://example.com/association"),
            IsCollection = true,
            Name = "DeferredNavLink",
            Url = new Uri("http://example.com/navigation")
        };

        private readonly ODataNestedResourceInfo containedCollectionNavLinkWithPayloadMetadata = new ODataNestedResourceInfo()
        {
            AssociationLinkUrl = new Uri("http://example.com/expanded/association"),
            IsCollection = true,
            Name = "ContainedNavProp",
            Url = new Uri("http://example.com/expanded/navigation")
        };

        private readonly ODataNestedResourceInfo derivedContainedCollectionNavLinkWithPayloadMetadata = new ODataNestedResourceInfo()
        {
            AssociationLinkUrl = new Uri("http://example.com/expanded/association"),
            IsCollection = true,
            Name = "DerivedContainedNavProp",
            Url = new Uri("http://example.com/expanded/navigation")
        };

        private readonly ODataNestedResourceInfo containedNavLinkWithPayloadMetadata = new ODataNestedResourceInfo()
        {
            AssociationLinkUrl = new Uri("http://example.com/expanded/association"),
            IsCollection = false,
            Name = "ContainedNonCollectionNavProp",
            Url = new Uri("http://example.com/expanded/navigation")
        };

        private readonly ODataNestedResourceInfo expandedNavLinkWithPayloadMetadata = new ODataNestedResourceInfo()
        {
            AssociationLinkUrl = new Uri("http://example.com/expanded/association"),
            IsCollection = true,
            Name = "ExpandedNavLink",
            Url = new Uri("http://example.com/expanded/navigation")
        };

        private readonly ODataNestedResourceInfo navLinkWithoutPayloadMetadata = new ODataNestedResourceInfo
        {
            IsCollection = true,
            Name = "DeferredNavLink",
        };

        private readonly ODataNestedResourceInfo expandedNavLinkWithoutPayloadMetadata = new ODataNestedResourceInfo()
        {
            IsCollection = true,
            Name = "ExpandedNavLink",
        };

        private readonly ODataNestedResourceInfo unknownNonCollectionNavLinkWithPayloadMetadata = new ODataNestedResourceInfo()
        {
            AssociationLinkUrl = new Uri("http://example.com/expanded/association"),
            IsCollection = false,
            Name = "UnknownNonCollectionNavProp",
            Url = new Uri("http://example.com/expanded/navigation")
        };

        private readonly ODataNestedResourceInfo unknownCollectionNavLinkWithPayloadMetadata = new ODataNestedResourceInfo()
        {
            AssociationLinkUrl = new Uri("http://example.com/expanded/association"),
            IsCollection = true,
            Name = "UnknownCollectionNavProp",
            Url = new Uri("http://example.com/expanded/navigation")
        };

        private static readonly EdmEntityType EntityType;
        private static readonly EdmEntityType DerivedType;
        private static readonly EdmEntityType AnotherEntityType;
        private static readonly EdmEntityType EnumAsKeyEntityType;
        private static readonly EdmEntitySet EntitySet;
        private static readonly EdmEnumType Gender;
        private static readonly EdmEntitySet AnotherEntitySet;
        private static readonly EdmModel Model;

        private const string PayloadWithNoMetadata = "{\"ID\":123,\"ExpandedNavLink\":[]}";

        private const string PayloadMetadataWithoutOpeningBrace =
                "\"@odata.id\":\"http://example.com/id\"," +
                "\"@odata.etag\":\"etag\"," +
                "\"@odata.editLink\":\"http://example.com/edit\"," +
                "\"@odata.readLink\":\"http://example.com/read\"," +
                "\"@odata.mediaEditLink\":\"http://example.com/mr/edit\"," +
                "\"@odata.mediaReadLink\":\"http://example.com/mr/read\"," +
                "\"@odata.mediaContentType\":\"image/png\"," +
                "\"@odata.mediaEtag\":\"mr etag\"," +
                "\"ID\":123," +
                "\"StreamProp1@odata.mediaEditLink\":\"http://example.com/stream/edit\"," +
                "\"StreamProp1@odata.mediaReadLink\":\"http://example.com/stream/read\"," +
                "\"StreamProp1@odata.mediaContentType\":\"image/jpeg\"," +
                "\"StreamProp1@odata.mediaEtag\":\"stream etag\"," +
                "\"DeferredNavLink@odata.associationLink\":\"http://example.com/association\"," +
                "\"DeferredNavLink@odata.navigationLink\":\"http://example.com/navigation\"," +
                "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/expanded/association\"," +
                "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/expanded/navigation\"," +
                "\"ExpandedNavLink\":[]," +
                "\"#Action\":{\"title\":\"ActionTitle\",\"target\":\"http://example.com/DoAction\"}," +
                "\"#Function\":{\"title\":\"FunctionTitle\",\"target\":\"http://example.com/DoFunction\"}" +
            "}";

        private const string PayloadWithAllMetadataExceptODataDotContext = "{" + PayloadMetadataWithoutOpeningBrace;
        private const string PayloadWithAllMetadata =
            "{" +
            "\"@odata.context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
            PayloadMetadataWithoutOpeningBrace;

        private const string PayloadMetadataWithoutOpeningBraceODataSimplified =
            "\"@id\":\"http://example.com/id\"," +
            "\"@etag\":\"etag\"," +
            "\"@editLink\":\"http://example.com/edit\"," +
            "\"@readLink\":\"http://example.com/read\"," +
            "\"@mediaEditLink\":\"http://example.com/mr/edit\"," +
            "\"@mediaReadLink\":\"http://example.com/mr/read\"," +
            "\"@mediaContentType\":\"image/png\"," +
            "\"@mediaEtag\":\"mr etag\"," +
            "\"ID\":123," +
            "\"StreamProp1@mediaEditLink\":\"http://example.com/stream/edit\"," +
            "\"StreamProp1@mediaReadLink\":\"http://example.com/stream/read\"," +
            "\"StreamProp1@mediaContentType\":\"image/jpeg\"," +
            "\"StreamProp1@mediaEtag\":\"stream etag\"," +
            "\"DeferredNavLink@associationLink\":\"http://example.com/association\"," +
            "\"DeferredNavLink@navigationLink\":\"http://example.com/navigation\"," +
            "\"ExpandedNavLink@associationLink\":\"http://example.com/expanded/association\"," +
            "\"ExpandedNavLink@navigationLink\":\"http://example.com/expanded/navigation\"," +
            "\"ExpandedNavLink\":[]," +
            "\"#Action\":{\"title\":\"ActionTitle\",\"target\":\"http://example.com/DoAction\"}," +
            "\"#Function\":{\"title\":\"FunctionTitle\",\"target\":\"http://example.com/DoFunction\"}" +
        "}";

        const string expectedPayloadWithFullMetadata = "{" +
                           "\"@odata.context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
                           "\"@odata.id\":\"http://example.com/id\"," +
                           "\"@odata.etag\":\"etag\"," +
                           "\"@odata.editLink\":\"http://example.com/edit\"," +
                           "\"@odata.readLink\":\"http://example.com/read\"," +
                           "\"@odata.mediaEditLink\":\"http://example.com/mr/edit\"," +
                           "\"@odata.mediaReadLink\":\"http://example.com/mr/read\"," +
                           "\"@odata.mediaContentType\":\"image/png\"," +
                           "\"@odata.mediaEtag\":\"mr etag\"," +
                           "\"ID\":123," +
                           "\"StreamProp1@odata.mediaEditLink\":\"http://example.com/stream/edit\"," +
                           "\"StreamProp1@odata.mediaReadLink\":\"http://example.com/stream/read\"," +
                           "\"StreamProp1@odata.mediaContentType\":\"image/jpeg\"," +
                           "\"StreamProp1@odata.mediaEtag\":\"stream etag\"," +
                           "\"DeferredNavLink@odata.associationLink\":\"http://example.com/association\"," +
                           "\"DeferredNavLink@odata.navigationLink\":\"http://example.com/navigation\"," +
                           "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/expanded/association\"," +
                           "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/expanded/navigation\"," +
                           "\"ExpandedNavLink\":[]," +
                           "\"NavLinkDeclaredOnlyInModel@odata.associationLink\":\"http://example.com/read/NavLinkDeclaredOnlyInModel/$ref\"," +
                           "\"NavLinkDeclaredOnlyInModel@odata.navigationLink\":\"http://example.com/read/NavLinkDeclaredOnlyInModel\"," +
                           "\"ContainedNavProp@odata.associationLink\":\"http://example.com/read/ContainedNavProp/$ref\"," +
                           "\"ContainedNavProp@odata.navigationLink\":\"http://example.com/read/ContainedNavProp\"," +
                           "\"ContainedNonCollectionNavProp@odata.associationLink\":\"http://example.com/read/ContainedNonCollectionNavProp/$ref\"," +
                           "\"ContainedNonCollectionNavProp@odata.navigationLink\":\"http://example.com/read/ContainedNonCollectionNavProp\"," +
                           "\"AnotherContainedNavProp@odata.associationLink\":\"http://example.com/read/AnotherContainedNavProp/$ref\"," +
                           "\"AnotherContainedNavProp@odata.navigationLink\":\"http://example.com/read/AnotherContainedNavProp\"," +
                           "\"AnotherContainedNonCollectionNavProp@odata.associationLink\":\"http://example.com/read/AnotherContainedNonCollectionNavProp/$ref\"," +
                           "\"AnotherContainedNonCollectionNavProp@odata.navigationLink\":\"http://example.com/read/AnotherContainedNonCollectionNavProp\"," +
                           "\"UnknownNonCollectionNavProp@odata.associationLink\":\"http://example.com/read/UnknownNonCollectionNavProp/$ref\"," +
                           "\"UnknownNonCollectionNavProp@odata.navigationLink\":\"http://example.com/read/UnknownNonCollectionNavProp\"," +
                           "\"UnknownCollectionNavProp@odata.associationLink\":\"http://example.com/read/UnknownCollectionNavProp/$ref\"," +
                           "\"UnknownCollectionNavProp@odata.navigationLink\":\"http://example.com/read/UnknownCollectionNavProp\"," +
                           "\"EnumAsKeyContainedNavProp@odata.associationLink\":\"http://example.com/read/EnumAsKeyContainedNavProp/$ref\"," +
                           "\"EnumAsKeyContainedNavProp@odata.navigationLink\":\"http://example.com/read/EnumAsKeyContainedNavProp\"," +
                           "\"StreamProp2@odata.mediaEditLink\":\"http://example.com/edit/StreamProp2\"," +
                           "\"StreamProp2@odata.mediaReadLink\":\"http://example.com/read/StreamProp2\"," +
                           "\"#Action\":{\"title\":\"ActionTitle\",\"target\":\"http://example.com/DoAction\"}," +
                           "\"#Namespace.AlwaysBindableAction1\":{\"title\":\"Namespace.AlwaysBindableAction1\",\"target\":\"http://example.com/edit/Namespace.AlwaysBindableAction1\"}," +
                           "\"#Namespace.AlwaysBindableAction2\":{\"title\":\"Namespace.AlwaysBindableAction2\",\"target\":\"http://example.com/edit/Namespace.AlwaysBindableAction2\"}," +
                           "\"#Function\":{\"title\":\"FunctionTitle\",\"target\":\"http://example.com/DoFunction\"}," +
                           "\"#Namespace.AlwaysBindableFunction1\":{\"title\":\"Namespace.AlwaysBindableFunction1\",\"target\":\"http://example.com/edit/Namespace.AlwaysBindableFunction1\"}," +
                           "\"#Namespace.AlwaysBindableFunction2\":{\"title\":\"Namespace.AlwaysBindableFunction2\",\"target\":\"http://example.com/edit/Namespace.AlwaysBindableFunction2\"}," +
                           "\"#Namespace.Function3\":{\"title\":\"Namespace.Function3\",\"target\":\"http://example.com/edit/Namespace.Function3\"}," +
                           "\"#Namespace.Function4\":{\"title\":\"Namespace.Function4\",\"target\":\"http://example.com/edit/Namespace.Function4\"}" +
                           "}";

        const string expectedPayloadWithFullMetadataODataSimplified = "{" +
                           "\"@context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
                           "\"@id\":\"http://example.com/id\"," +
                           "\"@etag\":\"etag\"," +
                           "\"@editLink\":\"http://example.com/edit\"," +
                           "\"@readLink\":\"http://example.com/read\"," +
                           "\"@mediaEditLink\":\"http://example.com/mr/edit\"," +
                           "\"@mediaReadLink\":\"http://example.com/mr/read\"," +
                           "\"@mediaContentType\":\"image/png\"," +
                           "\"@mediaEtag\":\"mr etag\"," +
                           "\"ID\":123," +
                           "\"StreamProp1@mediaEditLink\":\"http://example.com/stream/edit\"," +
                           "\"StreamProp1@mediaReadLink\":\"http://example.com/stream/read\"," +
                           "\"StreamProp1@mediaContentType\":\"image/jpeg\"," +
                           "\"StreamProp1@mediaEtag\":\"stream etag\"," +
                           "\"DeferredNavLink@associationLink\":\"http://example.com/association\"," +
                           "\"DeferredNavLink@navigationLink\":\"http://example.com/navigation\"," +
                           "\"ExpandedNavLink@associationLink\":\"http://example.com/expanded/association\"," +
                           "\"ExpandedNavLink@navigationLink\":\"http://example.com/expanded/navigation\"," +
                           "\"ExpandedNavLink\":[]," +
                           "\"NavLinkDeclaredOnlyInModel@associationLink\":\"http://example.com/read/NavLinkDeclaredOnlyInModel/$ref\"," +
                           "\"NavLinkDeclaredOnlyInModel@navigationLink\":\"http://example.com/read/NavLinkDeclaredOnlyInModel\"," +
                           "\"ContainedNavProp@associationLink\":\"http://example.com/read/ContainedNavProp/$ref\"," +
                           "\"ContainedNavProp@navigationLink\":\"http://example.com/read/ContainedNavProp\"," +
                           "\"ContainedNonCollectionNavProp@associationLink\":\"http://example.com/read/ContainedNonCollectionNavProp/$ref\"," +
                           "\"ContainedNonCollectionNavProp@navigationLink\":\"http://example.com/read/ContainedNonCollectionNavProp\"," +
                           "\"AnotherContainedNavProp@associationLink\":\"http://example.com/read/AnotherContainedNavProp/$ref\"," +
                           "\"AnotherContainedNavProp@navigationLink\":\"http://example.com/read/AnotherContainedNavProp\"," +
                           "\"AnotherContainedNonCollectionNavProp@associationLink\":\"http://example.com/read/AnotherContainedNonCollectionNavProp/$ref\"," +
                           "\"AnotherContainedNonCollectionNavProp@navigationLink\":\"http://example.com/read/AnotherContainedNonCollectionNavProp\"," +
                           "\"UnknownNonCollectionNavProp@associationLink\":\"http://example.com/read/UnknownNonCollectionNavProp/$ref\"," +
                           "\"UnknownNonCollectionNavProp@navigationLink\":\"http://example.com/read/UnknownNonCollectionNavProp\"," +
                           "\"UnknownCollectionNavProp@associationLink\":\"http://example.com/read/UnknownCollectionNavProp/$ref\"," +
                           "\"UnknownCollectionNavProp@navigationLink\":\"http://example.com/read/UnknownCollectionNavProp\"," +
                           "\"EnumAsKeyContainedNavProp@associationLink\":\"http://example.com/read/EnumAsKeyContainedNavProp/$ref\"," +
                           "\"EnumAsKeyContainedNavProp@navigationLink\":\"http://example.com/read/EnumAsKeyContainedNavProp\"," +
                           "\"StreamProp2@mediaEditLink\":\"http://example.com/edit/StreamProp2\"," +
                           "\"StreamProp2@mediaReadLink\":\"http://example.com/read/StreamProp2\"," +
                           "\"#Action\":{\"title\":\"ActionTitle\",\"target\":\"http://example.com/DoAction\"}," +
                           "\"#Namespace.AlwaysBindableAction1\":{\"title\":\"Namespace.AlwaysBindableAction1\",\"target\":\"http://example.com/edit/Namespace.AlwaysBindableAction1\"}," +
                           "\"#Namespace.AlwaysBindableAction2\":{\"title\":\"Namespace.AlwaysBindableAction2\",\"target\":\"http://example.com/edit/Namespace.AlwaysBindableAction2\"}," +
                           "\"#Function\":{\"title\":\"FunctionTitle\",\"target\":\"http://example.com/DoFunction\"}," +
                           "\"#Namespace.AlwaysBindableFunction1\":{\"title\":\"Namespace.AlwaysBindableFunction1\",\"target\":\"http://example.com/edit/Namespace.AlwaysBindableFunction1\"}," +
                           "\"#Namespace.AlwaysBindableFunction2\":{\"title\":\"Namespace.AlwaysBindableFunction2\",\"target\":\"http://example.com/edit/Namespace.AlwaysBindableFunction2\"}," +
                           "\"#Namespace.Function3\":{\"title\":\"Namespace.Function3\",\"target\":\"http://example.com/edit/Namespace.Function3\"}," +
                           "\"#Namespace.Function4\":{\"title\":\"Namespace.Function4\",\"target\":\"http://example.com/edit/Namespace.Function4\"}" +
                           "}";

        private const string PayloadWithAllMetadataODataSimplified =
            "{" +
            "\"@context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
            PayloadMetadataWithoutOpeningBraceODataSimplified;

        private ODataResource entryWithOnlyData;
        private ODataResource entryWithOnlyData2;
        private ODataResource entryWithOnlyData3;
        private ODataResource derivedEntry;

        public AutoComputePayloadMetadataInJsonIntegrationTests()
        {
            entryWithPayloadMetadata.AddAction(new ODataAction { Metadata = new Uri("http://example.com/$metadata#Action"), Target = new Uri("http://example.com/DoAction"), Title = "ActionTitle" });
            entryWithPayloadMetadata.AddFunction(new ODataFunction() { Metadata = new Uri("http://example.com/$metadata#Function"), Target = new Uri("http://example.com/DoFunction"), Title = "FunctionTitle" });

            this.entryWithOnlyData = new ODataResource { Properties = new[] { new ODataProperty { Name = "ID", Value = 123 }, new ODataProperty { Name = "Name", Value = "Bob" } }, };
            this.entryWithOnlyData2 = new ODataResource { Properties = new[] { new ODataProperty { Name = "ID", Value = 234 }, new ODataProperty { Name = "Name", Value = "Foo" } }, };
            this.entryWithOnlyData3 = new ODataResource { Properties = new[] { new ODataProperty { Name = "ID", Value = 345 }, new ODataProperty { Name = "Name", Value = "Bar" } }, };

            this.derivedEntry = new ODataResource
            {
                TypeName = "Namespace.DerivedType",
                Properties = new[] { new ODataProperty { Name = "ID", Value = 345 }, new ODataProperty { Name = "Name", Value = "Bar" } },
            };
        }

        static AutoComputePayloadMetadataInJsonIntegrationTests()
        {
            Gender = new EdmEnumType("Namespace", "Gender");
            Gender.AddMember(new EdmEnumMember(Gender, "Male", new EdmEnumMemberValue(0)));
            Gender.AddMember(new EdmEnumMember(Gender, "Female", new EdmEnumMemberValue(1)));

            EntityType = new EdmEntityType("Namespace", "EntityType", null, false, false, true);
            EntityType.AddKeys(EntityType.AddStructuralProperty("ID", EdmPrimitiveTypeKind.Int32));
            EntityType.AddStructuralProperty("StreamProp1", EdmPrimitiveTypeKind.Stream);
            EntityType.AddStructuralProperty("StreamProp2", EdmPrimitiveTypeKind.Stream);
            IEdmStructuralProperty nameProperty = EntityType.AddStructuralProperty("Name", EdmCoreModel.Instance.GetString(isNullable: true), null);
            DerivedType = new EdmEntityType("Namespace", "DerivedType", EntityType, false, true);
            AnotherEntityType = new EdmEntityType("Namespace", "AnotherEntityType", null, false, false, true);
            AnotherEntityType.AddKeys(AnotherEntityType.AddStructuralProperty("ID", EdmPrimitiveTypeKind.Int32));
            AnotherEntityType.AddStructuralProperty("Name", EdmCoreModel.Instance.GetString(isNullable: true), null);

            EnumAsKeyEntityType = new EdmEntityType("Namespace", "EnumAsKeyEntityType");
            EnumAsKeyEntityType.AddKeys(EnumAsKeyEntityType.AddStructuralProperty("GenderID", new EdmEnumTypeReference(Gender, false)));

            var deferredNavLinkProp = EntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = EntityType,
                TargetMultiplicity = EdmMultiplicity.Many,
                Name = "DeferredNavLink"
            });

            var expandedNavLinkProp = EntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = EntityType,
                TargetMultiplicity = EdmMultiplicity.Many,
                Name = "ExpandedNavLink"
            });

            var navLinkDeclaredOnlyInModelProp = EntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = EntityType,
                TargetMultiplicity = EdmMultiplicity.Many,
                Name = "NavLinkDeclaredOnlyInModel"
            });

            EntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = EntityType,
                TargetMultiplicity = EdmMultiplicity.Many,
                Name = "ContainedNavProp",
                ContainsTarget = true
            });

            DerivedType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = EntityType,
                TargetMultiplicity = EdmMultiplicity.Many,
                Name = "DerivedContainedNavProp",
                ContainsTarget = true
            });

            EntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = EntityType,
                TargetMultiplicity = EdmMultiplicity.One,
                Name = "ContainedNonCollectionNavProp",
                ContainsTarget = true
            });

            EntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = AnotherEntityType,
                TargetMultiplicity = EdmMultiplicity.Many,
                Name = "AnotherContainedNavProp",
                ContainsTarget = true
            });

            EntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = AnotherEntityType,
                TargetMultiplicity = EdmMultiplicity.One,
                Name = "AnotherContainedNonCollectionNavProp",
                ContainsTarget = true
            });

            EntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = EntityType,
                TargetMultiplicity = EdmMultiplicity.One,
                Name = "UnknownNonCollectionNavProp",
                ContainsTarget = false
            });

            EntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = EntityType,
                TargetMultiplicity = EdmMultiplicity.Many,
                Name = "UnknownCollectionNavProp",
                ContainsTarget = false
            });

            // contained on derived
            DerivedType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = AnotherEntityType,
                TargetMultiplicity = EdmMultiplicity.Many,
                Name = "ContainedNavPropOnDerived",
                ContainsTarget = true
            });

            DerivedType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = AnotherEntityType,
                TargetMultiplicity = EdmMultiplicity.One,
                Name = "ContainedNonCollectionNavPropOnDerived",
                ContainsTarget = true
            });

            // contained is derived
            AnotherEntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = DerivedType,
                TargetMultiplicity = EdmMultiplicity.Many,
                Name = "ContainedNavPropIsDerived",
                ContainsTarget = true
            });

            AnotherEntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = DerivedType,
                TargetMultiplicity = EdmMultiplicity.One,
                Name = "ContainedNonCollectionNavPropIsDerived",
                ContainsTarget = true
            });

            EntityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo()
            {
                Target = EnumAsKeyEntityType,
                TargetMultiplicity = EdmMultiplicity.Many,
                Name = "EnumAsKeyContainedNavProp",
                ContainsTarget = true
            });

            var container = new EdmEntityContainer("Namespace", "Container");
            EntitySet = container.AddEntitySet("EntitySet", EntityType);

            EntitySet.AddNavigationTarget(deferredNavLinkProp, EntitySet);
            EntitySet.AddNavigationTarget(expandedNavLinkProp, EntitySet);
            EntitySet.AddNavigationTarget(navLinkDeclaredOnlyInModelProp, EntitySet);

            AnotherEntitySet = container.AddEntitySet("AnotherEntitySet", AnotherEntityType);

            Model = new EdmModel();
            Model.AddElement(Gender);
            Model.AddElement(EntityType);
            Model.AddElement(DerivedType);
            Model.AddElement(AnotherEntityType);
            Model.AddElement(EnumAsKeyEntityType);
            Model.AddElement(container);
            Model.SetOptimisticConcurrencyAnnotation(EntitySet, new[] { nameProperty });

            var alwaysBindableAction1 = new EdmAction("Namespace", "AlwaysBindableAction1", null /*returnType*/, true /*isBound*/, null /*entitySetPath*/);
            alwaysBindableAction1.AddParameter(new EdmOperationParameter(alwaysBindableAction1, "p", new EdmEntityTypeReference(EntityType, isNullable: true)));
            Model.AddElement(alwaysBindableAction1);
            var alwaysBindableActionImport1 = new EdmActionImport(container, "AlwaysBindableAction1", alwaysBindableAction1);
            container.AddElement(alwaysBindableActionImport1);

            var alwaysBindableAction2 = new EdmAction("Namespace", "AlwaysBindableAction2", null /*returnType*/, true /*isBound*/, null /*entitySetPath*/);
            alwaysBindableAction2.AddParameter(new EdmOperationParameter(alwaysBindableAction2, "p", new EdmEntityTypeReference(EntityType, isNullable: true)));
            Model.AddElement(alwaysBindableAction2);
            var alwaysBindableActionImport2 = new EdmActionImport(container, "AlwaysBindableAction2", alwaysBindableAction2);
            container.AddElement(alwaysBindableActionImport2);

            var action1 = new EdmAction("Namespace", "Action1", null /*returnType*/, false /*isBound*/, null /*entitySetPath*/);
            action1.AddParameter(new EdmOperationParameter(action1, "p", new EdmEntityTypeReference(EntityType, isNullable: true)));
            Model.AddElement(action1);
            var actionImport1 = new EdmActionImport(container, "Action1", action1);
            container.AddElement(actionImport1);

            var action2 = new EdmAction("Namespace", "Action1", null /*returnType*/, false /*isBound*/, null /*entitySetPath*/);
            action2.AddParameter(new EdmOperationParameter(action2, "p", new EdmEntityTypeReference(EntityType, isNullable: true)));
            Model.AddElement(action2);
            var actionImport2 = new EdmActionImport(container, "Action1", action2);
            container.AddElement(actionImport2);

            var alwaysBindableFunction1 = new EdmFunction("Namespace", "AlwaysBindableFunction1", EdmCoreModel.Instance.GetString(isNullable: true), true /*isBound*/, null /*entitySetPath*/, false /*iscomposable*/);
            alwaysBindableFunction1.AddParameter("p", new EdmEntityTypeReference(EntityType, isNullable: true));
            Model.AddElement(alwaysBindableFunction1);
            var alwaysBindableFunctionImport1 = new EdmFunctionImport(container, "AlwaysBindableFunction1", alwaysBindableFunction1);
            container.AddElement(alwaysBindableFunctionImport1);

            var alwaysBindableFunction2 = new EdmFunction("Namespace", "AlwaysBindableFunction2", EdmCoreModel.Instance.GetString(isNullable: true), true /*isBound*/, null /*entitySetPath*/, false /*iscomposable*/);
            alwaysBindableFunction2.AddParameter("p", new EdmEntityTypeReference(EntityType, isNullable: true));
            Model.AddElement(alwaysBindableFunction2);
            var alwaysBindableFunctionImport2 = new EdmFunctionImport(container, "AlwaysBindableFunction2", alwaysBindableFunction2);
            container.AddElement(alwaysBindableFunctionImport2);

            var function1 = new EdmFunction("Namespace", "Function1", EdmCoreModel.Instance.GetString(isNullable: true), false /*isBound*/, null /*entitySetPath*/, false /*iscomposable*/);
            function1.AddParameter("p", new EdmEntityTypeReference(EntityType, isNullable: true));
            Model.AddElement(function1);
            var functionImport1 = new EdmFunctionImport(container, "Function1", function1);
            container.AddElement(functionImport1);

            var function2 = new EdmFunction("Namespace", "Function2", EdmCoreModel.Instance.GetString(isNullable: true), false /*isBound*/, null /*entitySetPath*/, false /*iscomposable*/);
            function2.AddParameter("p", new EdmEntityTypeReference(EntityType, isNullable: true));
            Model.AddElement(function2);
            var functionImport2 = new EdmFunctionImport(container, "Function2", function2);
            container.AddElement(functionImport2);

            var function3 = new EdmFunction("Namespace", "Function3", new EdmEntityTypeReference(EntityType, false), true /*isBound*/, new EdmPathExpression("p/ContainedNonCollectionNavProp"), false /*iscomposable*/);
            function3.AddParameter("p", new EdmEntityTypeReference(EntityType, isNullable: true));
            Model.AddElement(function3);

            var function4 = new EdmFunction("Namespace", "Function4", new EdmEntityTypeReference(EntityType, false), true /*isBound*/, new EdmPathExpression("p/ExpandedNavLink"), true /*iscomposable*/);
            function4.AddParameter("p", new EdmEntityTypeReference(EntityType, isNullable: true));
            Model.AddElement(function4);
        }

        public enum SerializationType
        {
            NoSerializationInfo,
            ResourceSerializationInfo,
            ResourceAndPropertySerializationInfo
        }

        [Theory]
        [InlineData(SerializationType.NoSerializationInfo, /*fullMetadata*/ false)]
        [InlineData(SerializationType.ResourceAndPropertySerializationInfo, /*fullMetadata*/ false)]
        [InlineData(SerializationType.ResourceSerializationInfo, /*fullMetadata*/ false)]
        [InlineData(SerializationType.NoSerializationInfo, /*fullMetadata*/ true)]
        [InlineData(SerializationType.ResourceAndPropertySerializationInfo, /*fullMetadata*/ true)]
        [InlineData(SerializationType.ResourceSerializationInfo, /*fullMetadata*/ true)]
        public void WritingNestedResourceInfoWithVariousSerializationInfoSucceeds(SerializationType serializationType, bool fullMetadata)
        {
            // setup model
            var model = new EdmModel();
            var entityType = new EdmEntityType("NS", "entityType");
            entityType.AddKeys(
                entityType.AddStructuralProperty("keyProperty", EdmPrimitiveTypeKind.Int64));

            var nestedEntityType = new EdmEntityType("NS", "nestedEntityType");
            nestedEntityType.AddKeys(
                nestedEntityType.AddStructuralProperty("keyProperty", EdmPrimitiveTypeKind.Int64));

            entityType.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "nestedEntities",
                Target = nestedEntityType,
                TargetMultiplicity = EdmMultiplicity.Many,
                ContainsTarget = true
            });

            var container = new EdmEntityContainer("NS", "Container");
            var entitySet = container.AddEntitySet("EntitySet", entityType);

            model.AddElements(new IEdmSchemaElement[] { entityType, nestedEntityType, container });

            // setup writer
            string str;
            using (var stream = new MemoryStream())
            {
                var message = new InMemoryMessage { Stream = stream };
                message.SetHeader("Content-Type", fullMetadata ? "application/json;odata.metadata=full" : "application/json");
                var settings = new ODataMessageWriterSettings
                {
                    ODataUri = new ODataUri
                    {
                        ServiceRoot = new Uri("http://svc/"),
                        Path = new ODataUriParser(model, new Uri("EntitySet", UriKind.Relative)).ParsePath()
                    },
                };
                var writer = new ODataMessageWriter((IODataResponseMessage)message, settings, model);

                // write payload
                var entitySetWriter = writer.CreateODataResourceSetWriter(entitySet, entitySet.EntityType());
                entitySetWriter.WriteStart(new ODataResourceSet());
                var resource = new ODataResource
                {
                    Properties = new[]
                        {
                        new ODataProperty {
                            Name = "keyProperty",
                            Value = 1L,
                            SerializationInfo = serializationType == SerializationType.ResourceAndPropertySerializationInfo ?
                                new ODataPropertySerializationInfo {
                                    PropertyKind = ODataPropertyKind.Key
                                } : null
                        }
                    },
                    TypeName = serializationType == SerializationType.NoSerializationInfo ? null : entityType.FullName
                };

                if (serializationType != SerializationType.NoSerializationInfo)
                {
                    resource.SerializationInfo = new ODataResourceSerializationInfo
                    {
                        NavigationSourceName = "EntitySet",
                        NavigationSourceKind = EdmNavigationSourceKind.EntitySet,
                        NavigationSourceEntityTypeName = entityType.FullName,
                        ExpectedTypeName = entityType.FullName,
                        IsFromCollection = true
                    };
                }

                entitySetWriter.WriteStart(resource);
                entitySetWriter.WriteStart(
                    new ODataNestedResourceInfo
                    {
                        Name = "nestedEntities",
                    }
                );
                entitySetWriter.WriteStart(new ODataResourceSet());
                var entityValue = new ODataResource
                {
                    TypeName = "NS.nestedEntityType",
                    Properties = new[]
                    {
                    new ODataProperty { Name = "keyProperty", Value = 1L }
                }
                };
                entitySetWriter.WriteStart(entityValue);
                entitySetWriter.WriteEnd(); // nestedEntity
                entitySetWriter.WriteEnd(); // nested resourceSet
                entitySetWriter.WriteEnd(); // nestedInfo
                entitySetWriter.WriteEnd(); // entity
                entitySetWriter.WriteEnd(); // resourceSet
                str = Encoding.UTF8.GetString(stream.ToArray());
            }

            var typeAnnotation =
                serializationType == SerializationType.NoSerializationInfo ? "" :
                "\"@odata.type\":\"#NS.entityType\",";

            var expected = fullMetadata ?
             "{\"@odata.context\":\"http://svc/$metadata#EntitySet\"," +
                "\"value\":[{" +
                typeAnnotation +
                    "\"@odata.id\":\"EntitySet(1)\"," +
                    "\"@odata.editLink\":\"EntitySet(1)\"," +
                    "\"keyProperty@odata.type\":\"#Int64\"," +
                    "\"keyProperty\":1," +
                    "\"nestedEntities@odata.associationLink\":\"http://svc/EntitySet(1)/nestedEntities/$ref\"," +
                    "\"nestedEntities@odata.navigationLink\":\"http://svc/EntitySet(1)/nestedEntities\"," +
                    "\"nestedEntities\":[{" +
                        "\"@odata.type\":\"#NS.nestedEntityType\"," +
                        "\"@odata.id\":\"EntitySet(1)/nestedEntities(1)\"," +
                        "\"@odata.editLink\":\"EntitySet(1)/nestedEntities(1)\"," +
                        "\"keyProperty@odata.type\":\"#Int64\"," +
                        "\"keyProperty\":1" +
                    "}]" +
                   "}]}" :
             "{\"@odata.context\":\"http://svc/$metadata#EntitySet\"," +
                "\"value\":[{" +
                    "\"keyProperty\":1," +
                    "\"nestedEntities\":[{" +
                        "\"keyProperty\":1" +
                    "}]" +
                   "}]}";

            Assert.Equal(expected, str);
        }

        [Fact]
        public void WritingDynamicComplexPropertyWithModelSpecifiedInFullMetadataMode()
        {
            // setup model
            var model = new EdmModel();
            var complexType = new EdmComplexType("NS", "ComplexType");
            complexType.AddStructuralProperty("PrimitiveProperty1", EdmPrimitiveTypeKind.Int64);
            complexType.AddStructuralProperty("PrimitiveProperty2", EdmPrimitiveTypeKind.Int64);
            var entityType = new EdmEntityType("NS", "EntityType", null, false, true);
            entityType.AddKeys(
                entityType.AddStructuralProperty("PrimitiveProperty", EdmPrimitiveTypeKind.Int64));
            var container = new EdmEntityContainer("NS", "Container");
            var entitySet = container.AddEntitySet("EntitySet", entityType);
            model.AddElements(new IEdmSchemaElement[] { complexType, entityType, container });

            // setup writer
            var stream = new MemoryStream();
            var message = new InMemoryMessage { Stream = stream };
            message.SetHeader("Content-Type", "application/json;odata.metadata=full");
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri
                {
                    ServiceRoot = new Uri("http://svc/")
                },
            };
            var writer = new ODataMessageWriter((IODataResponseMessage)message, settings, model);

            // write payload
            var entitySetWriter = writer.CreateODataResourceSetWriter(entitySet);
            entitySetWriter.WriteStart(new ODataResourceSet());
            entitySetWriter.WriteStart(
                new ODataResource
                {
                    Properties = new[]
                    {
                        new ODataProperty { Name = "PrimitiveProperty", Value = 1L },
                        new ODataProperty
                        {
                            Name = "DynamicCollectionOfPrimitiveProperty",
                            Value = new ODataCollectionValue
                            {
                                TypeName = "Collection(Edm.Int64)",
                                Items = Enumerable.Range(0, 3).Select(x => (object)(long)x)
                            }
                        }
                    }
                }
            );
            entitySetWriter.WriteStart(
                new ODataNestedResourceInfo
                {
                    Name = "DynamicComplexProperty",
                    SerializationInfo = new ODataNestedResourceInfoSerializationInfo() { IsUndeclared = true }
                }
            );
            var complexValue = new ODataResource
            {
                TypeName = "NS.ComplexType",
                Properties = new[]
                {
                    new ODataProperty { Name = "PrimitiveProperty1", Value = 1L },
                    new ODataProperty { Name = "PrimitiveProperty2", Value = 2L }
                }
            };
            entitySetWriter.WriteStart(complexValue);
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteStart(
                new ODataNestedResourceInfo
                {
                    Name = "DyanmicCollectionOfComplexProperty",
                    IsCollection = true
                }
            );
            entitySetWriter.WriteStart(new ODataResourceSet { TypeName = "Collection(NS.ComplexType)" });
            entitySetWriter.WriteStart(complexValue);
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteStart(complexValue);
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteEnd();
            var str = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(
                "{\"@odata.context\":\"http://svc/$metadata#EntitySet\"," +
                "\"value\":[{" +
                    "\"@odata.id\":\"EntitySet(1)\"," +
                    "\"@odata.editLink\":\"EntitySet(1)\"," +
                    "\"PrimitiveProperty@odata.type\":\"#Int64\"," +
                    "\"PrimitiveProperty\":1," +
                    "\"DynamicCollectionOfPrimitiveProperty@odata.type\":\"#Collection(Int64)\"," +
                    "\"DynamicCollectionOfPrimitiveProperty\":[0,1,2]," +
                    "\"DynamicComplexProperty\":{" +
                        "\"@odata.type\":\"#NS.ComplexType\"," +
                        "\"PrimitiveProperty1@odata.type\":\"#Int64\"," +
                        "\"PrimitiveProperty1\":1," +
                        "\"PrimitiveProperty2@odata.type\":\"#Int64\"," +
                        "\"PrimitiveProperty2\":2" +
                    "}," +
                    "\"DyanmicCollectionOfComplexProperty@odata.type\":\"#Collection(NS.ComplexType)\"," +
                    "\"DyanmicCollectionOfComplexProperty\":[" +
                        "{" +
                            "\"@odata.type\":\"#NS.ComplexType\"," +
                            "\"PrimitiveProperty1@odata.type\":\"#Int64\"," +
                            "\"PrimitiveProperty1\":1," +
                            "\"PrimitiveProperty2@odata.type\":\"#Int64\"," +
                            "\"PrimitiveProperty2\":2" +
                        "}," +
                        "{" +
                            "\"@odata.type\":\"#NS.ComplexType\"," +
                            "\"PrimitiveProperty1@odata.type\":\"#Int64\"," +
                            "\"PrimitiveProperty1\":1," +
                            "\"PrimitiveProperty2@odata.type\":\"#Int64\"," +
                            "\"PrimitiveProperty2\":2" +
                        "}]}]}", str);
        }

        [Fact]
        public void WritingDynamicComplexPropertyWithModelSpecifiedInFullMetadataMode_401()
        {
            // setup model
            var model = new EdmModel();
            var complexType = new EdmComplexType("NS", "ComplexType");
            complexType.AddStructuralProperty("PrimitiveProperty1", EdmPrimitiveTypeKind.Int64);
            complexType.AddStructuralProperty("PrimitiveProperty2", EdmPrimitiveTypeKind.Int64);
            var entityType = new EdmEntityType("NS", "EntityType", null, false, true);
            entityType.AddKeys(
                entityType.AddStructuralProperty("PrimitiveProperty", EdmPrimitiveTypeKind.Int64));
            var container = new EdmEntityContainer("NS", "Container");
            var entitySet = container.AddEntitySet("EntitySet", entityType);
            model.AddElements(new IEdmSchemaElement[] { complexType, entityType, container });

            // setup writer
            var stream = new MemoryStream();
            var message = new InMemoryMessage { Stream = stream };
            message.SetHeader("Content-Type", "application/json;odata.metadata=full");
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri
                {
                    ServiceRoot = new Uri("http://svc/")
                },
                Version = ODataVersion.V401
            };
            var writer = new ODataMessageWriter((IODataResponseMessage)message, settings, model);

            // write payload
            var entitySetWriter = writer.CreateODataResourceSetWriter(entitySet);
            entitySetWriter.WriteStart(new ODataResourceSet());
            entitySetWriter.WriteStart(
                new ODataResource
                {
                    Properties = new[]
                    {
                        new ODataProperty { Name = "PrimitiveProperty", Value = 1L },
                        new ODataProperty
                        {
                            Name = "DynamicCollectionOfPrimitiveProperty",
                            Value = new ODataCollectionValue
                            {
                                TypeName = "Collection(Edm.Int64)",
                                Items = Enumerable.Range(0, 3).Select(x => (object)(long)x)
                            }
                        }
                    }
                }
            );
            entitySetWriter.WriteStart(
                new ODataNestedResourceInfo
                {
                    Name = "DynamicComplexProperty",
                    SerializationInfo = new ODataNestedResourceInfoSerializationInfo() { IsUndeclared = true }
                }
            );
            var complexValue = new ODataResource
            {
                TypeName = "NS.ComplexType",
                Properties = new[]
                {
                    new ODataProperty { Name = "PrimitiveProperty1", Value = 1L },
                    new ODataProperty { Name = "PrimitiveProperty2", Value = 2L }
                }
            };
            entitySetWriter.WriteStart(complexValue);
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteStart(
                new ODataNestedResourceInfo
                {
                    Name = "DyanmicCollectionOfComplexProperty",
                    IsCollection = true
                }
            );
            entitySetWriter.WriteStart(new ODataResourceSet { TypeName = "Collection(NS.ComplexType)" });
            entitySetWriter.WriteStart(complexValue);
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteStart(complexValue);
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteEnd();
            entitySetWriter.WriteEnd();
            var str = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(
                "{\"@context\":\"http://svc/$metadata#EntitySet\"," +
                "\"value\":[{" +
                    "\"@id\":\"EntitySet(1)\"," +
                    "\"@editLink\":\"EntitySet(1)\"," +
                    "\"PrimitiveProperty@type\":\"Int64\"," +
                    "\"PrimitiveProperty\":1," +
                    "\"DynamicCollectionOfPrimitiveProperty@type\":\"Collection(Int64)\"," +
                    "\"DynamicCollectionOfPrimitiveProperty\":[0,1,2]," +
                    "\"DynamicComplexProperty\":{" +
                        "\"@type\":\"#NS.ComplexType\"," +
                        "\"PrimitiveProperty1@type\":\"Int64\"," +
                        "\"PrimitiveProperty1\":1," +
                        "\"PrimitiveProperty2@type\":\"Int64\"," +
                        "\"PrimitiveProperty2\":2" +
                    "}," +
                    "\"DyanmicCollectionOfComplexProperty@type\":\"#Collection(NS.ComplexType)\"," +
                    "\"DyanmicCollectionOfComplexProperty\":[" +
                        "{" +
                            "\"@type\":\"#NS.ComplexType\"," +
                            "\"PrimitiveProperty1@type\":\"Int64\"," +
                            "\"PrimitiveProperty1\":1," +
                            "\"PrimitiveProperty2@type\":\"Int64\"," +
                            "\"PrimitiveProperty2\":2" +
                        "}," +
                        "{" +
                            "\"@type\":\"#NS.ComplexType\"," +
                            "\"PrimitiveProperty1@type\":\"Int64\"," +
                            "\"PrimitiveProperty1\":1," +
                            "\"PrimitiveProperty2@type\":\"Int64\"," +
                            "\"PrimitiveProperty2\":2" +
                        "}]}]}", str);
        }

        #region Entity Reference link
        [Fact]
        public void WritingNestedSingleEntityReferenceLinkInRequest_ODataV4_Works()
        {
            // Arrange & Act
            string payload = GetEntityReferenceLinksPayload(ODataVersion.V4, request: true, collection: false);

            // Assert
            Assert.Equal("{\"@odata.context\":\"http://svc/$metadata#Customers/$entity\"," +
                "\"CustomerId\":7," +
                "\"BillOrder@odata.bind\":\"http://svc/Orders(8)\"" +
              "}", payload);
        }

        [Fact]
        public void WritingNestedSingleEntityReferenceLinkInResponse_ODataV4_Works()
        {
            // Arrange & Act
            string payload = GetEntityReferenceLinksPayload(ODataVersion.V4, request: false, collection: false);

            // Assert
            Assert.Equal("{\"@odata.context\":\"http://svc/$metadata#Customers/$entity\"," +
                "\"CustomerId\":7," +
                "\"BillOrder\":{" +
                    "\"@odata.id\":\"Orders(8)\"" +
                  "}" +
                "}", payload);
        }

        [Fact]
        public void WritingNestedSingleEntityReferenceLinkInRequest_ODataV401_Works()
        {
            // Arrange & Act
            string payload = GetEntityReferenceLinksPayload(ODataVersion.V401, request: true, collection: false);

            // Assert
            Assert.Equal("{\"@context\":\"http://svc/$metadata#Customers/$entity\"," +
                "\"CustomerId\":7," +
                "\"BillOrder\":{" +
                    "\"@id\":\"Orders(8)\"" +
                  "}" +
                "}", payload);
        }

        [Fact]
        public void WritingNestedSingleEntityReferenceLinkInResponse_ODataV401_Works()
        {
            // Arrange & Act
            string payload = GetEntityReferenceLinksPayload(ODataVersion.V401, request: false, collection: false);

            // Assert
            Assert.Equal("{\"@context\":\"http://svc/$metadata#Customers/$entity\"," +
                "\"CustomerId\":7," +
                "\"BillOrder\":{" +
                    "\"@id\":\"Orders(8)\"" +
                  "}" +
                "}", payload);
        }

        [Fact]
        public void WritingNestedCollectionEntityReferenceLinksInRequest_ODataV4_Works()
        {
            // Arrange & Act
            string payload = GetEntityReferenceLinksPayload(ODataVersion.V4, request: true, collection: true);

            // Assert
            Assert.Equal("{\"@odata.context\":\"http://svc/$metadata#Customers/$entity\"," +
                "\"CustomerId\":7," +
                "\"BillOrders@odata.bind\":[" +
                  "\"http://svc/Orders(2)\"," +
                  "\"http://svc/Orders(3)\"," +
                  "\"http://svc/Orders(4)\"" +
                "]" +
              "}", payload);
        }

        [Fact]
        public void WritingNestedCollectionEntityReferenceLinksInResponse_ODataV4_Works()
        {
            // Arrange & Act
            string payload = GetEntityReferenceLinksPayload(ODataVersion.V4, request: false, collection: true);

            // Assert
            Assert.Equal("{\"@odata.context\":\"http://svc/$metadata#Customers/$entity\"," +
                "\"CustomerId\":7," +
                "\"BillOrders\":[" +
                    "{\"@odata.id\":\"Orders(2)\"}," +
                    "{\"@odata.id\":\"Orders(3)\"}," +
                    "{\"@odata.id\":\"Orders(4)\"}" +
                  "]" +
                "}", payload);
        }

        [Fact]
        public void WritingNestedCollectionEntityReferenceLinksInRequest_ODataV401_Works()
        {
            // Arrange & Act
            string payload = GetEntityReferenceLinksPayload(ODataVersion.V401, request: true, collection: true);

            // Assert
            Assert.Equal("{\"@context\":\"http://svc/$metadata#Customers/$entity\"," +
                "\"CustomerId\":7," +
                "\"BillOrders\":[" +
                    "{\"@id\":\"Orders(2)\"}," +
                    "{\"@id\":\"Orders(3)\"}," +
                    "{\"@id\":\"Orders(4)\"}" +
                  "]" +
                "}", payload);
        }

        [Fact]
        public void WritingNestedCollectionEntityReferenceLinksInResponse_ODataV401_Works()
        {
            // Arrange & Act
            string payload = GetEntityReferenceLinksPayload(ODataVersion.V401, request: false, collection: true);

            // Assert
            Assert.Equal("{\"@context\":\"http://svc/$metadata#Customers/$entity\"," +
                "\"CustomerId\":7," +
                "\"BillOrders\":[" +
                    "{\"@id\":\"Orders(2)\"}," +
                    "{\"@id\":\"Orders(3)\"}," +
                    "{\"@id\":\"Orders(4)\"}" +
                  "]" +
                "}", payload);
        }

        [Fact]
        public void WritingNestedCollectionEntityReferenceLinksInResponseWithoutNestedResourceSet_Throws()
        {
            // Arrange
            MemoryStream stream = new MemoryStream();
            ODataWriter writer = GetODataWriterForEntityReferenceLink(ODataVersion.V401, request: false, stream: stream);

            // Act
            Action test = () =>
            {
                writer.WriteStart(new ODataResource { Properties = new[] { new ODataProperty { Name = "CustomerId", Value = 7 } } });
                writer.WriteStart(new ODataNestedResourceInfo { Name = "BillOrder", IsCollection = false });
                writer.WriteEntityReferenceLink(new ODataEntityReferenceLink { Url = new Uri("http://svc/Orders(2)") });
                writer.WriteEntityReferenceLink(new ODataEntityReferenceLink { Url = new Uri("http://svc/Orders(3)") });
                writer.WriteEnd();
                writer.WriteEnd();
            };

            // Assert
            var exception = Assert.Throws<ODataException>(test);
            Assert.Equal(Strings.ODataWriterCore_MultipleItemsInNestedResourceInfoWithContent, exception.Message);
        }

        [Fact]
        public void WritingNestedCollectionEntityReferenceLinksInResponseWithNextLinks_ODataV40_Works()
        {
            // Arrange & Act
            Func<ODataResourceSet> func = () => new ODataResourceSet
            {
                NextPageLink = new Uri("http://nextLink")
            };

            MemoryStream stream = new MemoryStream();
            string actual = GetEntityReferenceLinksPayload(ODataVersion.V4, request: false, collection: true, nestedResourceSetFunc: func);

            // Assert
            Assert.Equal("{\"@odata.context\":\"http://svc/$metadata#Customers/$entity\"," +
                    "\"CustomerId\":7," +
                    "\"BillOrders@odata.nextLink\":\"http://nextlink/\"," +
                    "\"BillOrders\":[" +
                      "{\"@odata.id\":\"Orders(2)\"}," +
                      "{\"@odata.id\":\"Orders(3)\"}," +
                      "{\"@odata.id\":\"Orders(4)\"}" +
                    "]" +
                  "}", actual);
        }

        [Fact]
        public void WritingNestedCollectionEntityReferenceLinksInResponseWithNextLinks_ODataV401_Works()
        {
            // Arrange & Act
            Func<ODataResourceSet> func = () => new ODataResourceSet
            {
                NextPageLink = new Uri("http://nextLink")
            };

            MemoryStream stream = new MemoryStream();
            string actual = GetEntityReferenceLinksPayload(ODataVersion.V401, request: false, collection: true, nestedResourceSetFunc: func);

            // Assert
            Assert.Equal("{\"@context\":\"http://svc/$metadata#Customers/$entity\"," +
                    "\"CustomerId\":7," +
                    "\"BillOrders@nextLink\":\"http://nextlink/\"," +
                    "\"BillOrders\":[" +
                      "{\"@id\":\"Orders(2)\"}," +
                      "{\"@id\":\"Orders(3)\"}," +
                      "{\"@id\":\"Orders(4)\"}" +
                    "]" +
                  "}", actual);
        }

        private string GetEntityReferenceLinksPayload(ODataVersion version, bool request, bool collection,
            Func<ODataResourceSet> nestedResourceSetFunc = null)
        {
            MemoryStream stream = new MemoryStream();
            ODataWriter odataWriter = GetODataWriterForEntityReferenceLink(version, request, stream);

            odataWriter.WriteStart(new ODataResource { Properties = new[] { new ODataProperty { Name = "CustomerId", Value = 7 } } });
            if (collection)
            {
                odataWriter.WriteStart(new ODataNestedResourceInfo { Name = "BillOrders", IsCollection = true });
                if (!request)
                {
                    // only in response, we should call "WriteStart(ODataResourceSet)".
                    ODataResourceSet resourceSet = nestedResourceSetFunc == null ? new ODataResourceSet() : nestedResourceSetFunc();
                    odataWriter.WriteStart(resourceSet);
                }

                odataWriter.WriteEntityReferenceLink(new ODataEntityReferenceLink { Url = new Uri("http://svc/Orders(2)") });
                odataWriter.WriteEntityReferenceLink(new ODataEntityReferenceLink { Url = new Uri("http://svc/Orders(3)") });
                odataWriter.WriteEntityReferenceLink(new ODataEntityReferenceLink { Url = new Uri("http://svc/Orders(4)") });

                if (!request)
                {
                    odataWriter.WriteEnd(); // End of ResourceSet
                }
            }
            else
            {
                odataWriter.WriteStart(new ODataNestedResourceInfo { Name = "BillOrder", IsCollection = false });
                odataWriter.WriteEntityReferenceLink(new ODataEntityReferenceLink { Url = new Uri("http://svc/Orders(8)") });
            }

            odataWriter.WriteEnd(); // End of NestedResourceInfo
            odataWriter.WriteEnd(); // End of ResourceInfo
            odataWriter.Flush();

            return Encoding.UTF8.GetString(stream.ToArray());
        }

        private ODataWriter GetODataWriterForEntityReferenceLink(ODataVersion version, bool request, MemoryStream stream)
        {
            // Arrange - Edm model
            var model = new EdmModel();
            var customer = new EdmEntityType("NS", "Customer");
            customer.AddKeys(customer.AddStructuralProperty("CustomerId", EdmPrimitiveTypeKind.Int32));
            model.AddElement(customer);

            var order = new EdmEntityType("NS", "Order");
            order.AddKeys(order.AddStructuralProperty("OrderId", EdmPrimitiveTypeKind.Int32));
            model.AddElement(order);
            customer.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "BillOrder",
                Target = order,
                TargetMultiplicity = EdmMultiplicity.One
            });

            customer.AddUnidirectionalNavigation(new EdmNavigationPropertyInfo
            {
                Name = "BillOrders",
                Target = order,
                TargetMultiplicity = EdmMultiplicity.Many
            });

            var container = new EdmEntityContainer("NS", "Container");
            var customers = container.AddEntitySet("Customers", customer);
            model.AddElement(container);

            var message = new InMemoryMessage { Stream = stream };
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri
                {
                    ServiceRoot = new Uri("http://svc/")
                },
                Version = version
            };

            ODataMessageWriter writer;
            if (request)
            {
                writer = new ODataMessageWriter((IODataRequestMessage)message, settings, model);
            }
            else
            {
                writer = new ODataMessageWriter((IODataResponseMessage)message, settings, model);
            }

            // Act
            return writer.CreateODataResourceWriter(customers);
        }
        #endregion

        [Fact]
        public void WritingResourceNestedPropertyWithEdmAbstractTypeWorks()
        {
            EdmEntitySet entitySet;
            IEdmModel model = GetModelWithAbstractType(out entitySet);

            var stream = new MemoryStream();
            var message = new InMemoryMessage { Stream = stream };
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            var settings = new ODataMessageWriterSettings
            {
                ODataUri = new ODataUri
                {
                    ServiceRoot = new Uri("http://svc/")
                },
            };
            var writer = new ODataMessageWriter((IODataResponseMessage)message, settings, model);

            // write payload
            ODataResource resource = new ODataResource
            {
                TypeName = "NS.Customer",
                Properties = new[] { new ODataProperty { Name = "Id", Value = 9 } }
            };

            var resourceWriter = writer.CreateODataResourceWriter(entitySet);
            resourceWriter.WriteStart(resource);
            resourceWriter.WriteStart(new ODataNestedResourceInfo { Name = "ComplexProperty", IsCollection = false });
            ODataResource complexValue = new ODataResource
            {
                TypeName = "NS.Address",
                Properties = new[]
                {
                    new ODataProperty { Name = "City", Value = "Redmond" },
                    new ODataProperty { Name = "ZipCode", Value = 98052 },
                    new ODataProperty { Name = "Data", Value = new Date(2018, 12, 4) }
                }
            };
            resourceWriter.WriteStart(complexValue);
            resourceWriter.WriteEnd();
            resourceWriter.WriteEnd(); // end of "ComplexProperty" nested property

            resourceWriter.WriteStart(new ODataNestedResourceInfo { Name = "EntityProperty", IsCollection = false });
            ODataResource entityValue = new ODataResource
            {
                TypeName = "NS.Customer",
                Properties = new[] { new ODataProperty { Name = "Id", Value = 42 }}
            };
            resourceWriter.WriteStart(entityValue);
            resourceWriter.WriteEnd();
            resourceWriter.WriteEnd();// end of "EntityProperty" nested property
            resourceWriter.WriteEnd();

            var str = Encoding.UTF8.GetString(stream.ToArray());
            Assert.Equal(str, "{\"@odata.context\":\"http://svc/$metadata#Customers/$entity\"," +
                "\"Id\":9," +
                "\"ComplexProperty\":{" +
                    "\"@odata.type\":\"#NS.Address\"," +
                    "\"City\":\"Redmond\"," +
                    "\"ZipCode\":98052," +
                    "\"Data@odata.type\":\"#Date\"," +
                    "\"Data\":\"2018-12-04\"" +
                "}," +
                "\"EntityProperty\":{" +
                    "\"@odata.type\":\"#NS.Customer\"," +
                    "\"Id\":42" +
                "}}");
        }

        [Fact]
        public void ReadingResourceNestedPropertyWithEdmAbstractTypeWorks()
        {
            const string payload =
                "{\"@odata.context\":\"http://svc/$metadata#Customers/$entity\"," +
                "\"Id\":9," +
                "\"ComplexProperty\":{" +
                    "\"@odata.type\":\"#NS.Address\"," +
                    "\"City\":\"Redmond\"," +
                    "\"ZipCode\":98052," +
                    "\"Data@odata.type\":\"#Boolean\"," +
                    "\"Data\":true" +
                "}," +
                "\"EntityProperty\":{" +
                    "\"@odata.type\":\"#NS.Customer\"," +
                    "\"Id\":42" +
                "}}";

            EdmEntitySet entitySet;
            IEdmModel model = GetModelWithAbstractType(out entitySet);
            IEdmEntityType entityType = model.SchemaElements.OfType<IEdmEntityType>().First(c => c.Name == "Customer");

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelResource = null;
            IList<ODataResource> nestedResources = new List<ODataResource>();
            int deep = 0;
            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, model))
            {
                var reader = messageReader.CreateODataResourceReader(entitySet, entityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceStart:
                            deep++;
                            break;
                        case ODataReaderState.ResourceEnd:
                            if (deep == 2)
                            {
                                nestedResources.Add((ODataResource)reader.Item);
                            }
                            else
                            {
                                topLevelResource = (ODataResource)reader.Item;
                            }
                            deep--;
                            break;
                    }
                }
            }

            Assert.NotNull(topLevelResource);
            Assert.Equal(new Uri("http://svc/Customers(9)"), topLevelResource.Id);

            Assert.Equal(2, nestedResources.Count);
            Assert.Equal(new[] { "NS.Address", "NS.Customer" }, nestedResources.Select(n => n.TypeName));

            // #1
            ODataResource nestedResource = nestedResources.First(c => c.TypeName == "NS.Address");
            Assert.Equal(3, nestedResource.Properties.Count());
            Assert.Equal("Redmond", nestedResource.Properties.First(p => p.Name == "City").Value);
            Assert.Equal(98052, nestedResource.Properties.First(p => p.Name == "ZipCode").Value);
            Assert.Equal(true, nestedResource.Properties.First(p => p.Name == "Data").Value);

            // #2
            nestedResource = nestedResources.First(c => c.TypeName == "NS.Customer");
            Assert.Single(nestedResource.Properties);
            Assert.Equal(42, nestedResource.Properties.First(p => p.Name == "Id").Value);
        }

        private static IEdmModel GetModelWithAbstractType(out EdmEntitySet entitySet)
        {
            var model = new EdmModel();
            var complexType = new EdmComplexType("NS", "Address");
            complexType.AddStructuralProperty("City", EdmPrimitiveTypeKind.String);
            complexType.AddStructuralProperty("ZipCode", EdmPrimitiveTypeKind.Int32);
            complexType.AddStructuralProperty("Data", EdmPrimitiveTypeKind.PrimitiveType);
            var customer = new EdmEntityType("NS", "Customer");
            customer.AddKeys(customer.AddStructuralProperty("Id", EdmPrimitiveTypeKind.Int32));
            // <Property Name="ComplexProperty" Type="Edm.ComplexType" />
            customer.AddStructuralProperty("ComplexProperty", EdmCoreModel.Instance.GetComplexType(true));
            var container = new EdmEntityContainer("NS", "Container");
            entitySet = container.AddEntitySet("Customers", customer);
            model.AddElements(new IEdmSchemaElement[] { complexType, customer, container });

            IEdmEntityType entityType = EdmCoreModel.Instance.GetEntityType();
            // <NavigationProperty Name="EntityProperty" Type="Edm.EntityType" />
            var navProperty = customer.AddUnidirectionalNavigation(
                new EdmNavigationPropertyInfo()
                {
                    Target = entityType,
                    TargetMultiplicity = EdmMultiplicity.ZeroOrOne,
                    Name = "EntityProperty"
                });
            entitySet.AddNavigationTarget(navProperty, entitySet);
            return model;
        }

        [Fact]
        public void WritingSimplifiedODataAnnotationsInFullMetadataMode()
        {
            Assert.Equal(expectedPayloadWithFullMetadataODataSimplified,
                GetWriterOutputForEntryWithPayloadMetadata("application/json;odata.metadata=full", false, enableWritingODataAnnotationWithoutPrefix: true));
        }

        [Fact]
        public void WritingInNoMetadataModeShouldStripPayloadMetadataIfAutoComputePayloadMetadataInJsonIsTrue()
        {
            Assert.Equal(PayloadWithNoMetadata, GetWriterOutputForEntryWithPayloadMetadata("application/json;odata.metadata=none", true));
        }

        [Fact]
        public void WritingInNoMetadataModeShouldNotStripPayloadMetadataIfAutoComputePayloadMetadataInJsonIsFalse()
        {
            Assert.Equal(PayloadWithNoMetadata, GetWriterOutputForEntryWithPayloadMetadata("application/json;odata.metadata=none", false));
        }

        [Fact]
        public void WritingInMinimalMetadataModeShouldNotStripPayloadMetadataIfAutoComputePayloadMetadataInJsonIsTrue()
        {
            Assert.Equal(PayloadWithAllMetadata, GetWriterOutputForEntryWithPayloadMetadata("application/json;odata.metadata=minimal", true));
        }

        [Fact]
        public void WritingInMinimalMetadataModeShouldNotStripPayloadMetadataIfAutoComputePayloadMetadataInJsonIsFalse()
        {
            Assert.Equal(PayloadWithAllMetadata, GetWriterOutputForEntryWithPayloadMetadata("application/json;odata.metadata=minimal", false));
        }

        [Fact]
        public void WritingInFullMetadataModeShouldNotStripPayloadMetadataIfAutoComputePayloadMetadataInJsonIsFalse()
        {
            Assert.Equal(expectedPayloadWithFullMetadata, GetWriterOutputForEntryWithPayloadMetadata("application/json;odata.metadata=full", false));
        }

        [Fact]
        public void WritingInFullMetadataModeShouldNotStripPayloadMetadataAndShouldWriteMissingMetadataIfAutoComputePayloadMetadataInJsonIsTrue()
        {
            Assert.Equal(expectedPayloadWithFullMetadata, GetWriterOutputForEntryWithPayloadMetadata("application/json;odata.metadata=full", true));
        }

        [Fact]
        public void WritingInFullMetadataModeShouldWriteMissingMetadataForEntryWithOnlyDataIfAutoComputePayloadMetadataInJsonIsTrue()
        {
            const string expectedPayload = "{" +
                                           "\"@odata.context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
                                           "\"@odata.id\":\"EntitySet(123)\"," +
                                           "\"@odata.etag\":\"W/\\\"'Bob'\\\"\"," +
                                           "\"@odata.editLink\":\"EntitySet(123)\"," +
                                           "\"@odata.mediaEditLink\":\"EntitySet(123)/$value\"," +
                                           "\"ID\":123," +
                                           "\"Name\":\"Bob\"," +
                                           "\"DeferredNavLink@odata.associationLink\":\"http://example.com/EntitySet(123)/DeferredNavLink/$ref\"," +
                                           "\"DeferredNavLink@odata.navigationLink\":\"http://example.com/EntitySet(123)/DeferredNavLink\"," +
                                           "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/EntitySet(123)/ExpandedNavLink/$ref\"," +
                                           "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/EntitySet(123)/ExpandedNavLink\"," +
                                           "\"ExpandedNavLink\":[]," +
                                           "\"NavLinkDeclaredOnlyInModel@odata.associationLink\":\"http://example.com/EntitySet(123)/NavLinkDeclaredOnlyInModel/$ref\"," +
                                           "\"NavLinkDeclaredOnlyInModel@odata.navigationLink\":\"http://example.com/EntitySet(123)/NavLinkDeclaredOnlyInModel\"," +
                                           "\"ContainedNavProp@odata.associationLink\":\"http://example.com/EntitySet(123)/ContainedNavProp/$ref\"," +
                                           "\"ContainedNavProp@odata.navigationLink\":\"http://example.com/EntitySet(123)/ContainedNavProp\"," +
                                           "\"ContainedNonCollectionNavProp@odata.associationLink\":\"http://example.com/EntitySet(123)/ContainedNonCollectionNavProp/$ref\"," +
                                           "\"ContainedNonCollectionNavProp@odata.navigationLink\":\"http://example.com/EntitySet(123)/ContainedNonCollectionNavProp\"," +
                                           "\"AnotherContainedNavProp@odata.associationLink\":\"http://example.com/EntitySet(123)/AnotherContainedNavProp/$ref\"," +
                                           "\"AnotherContainedNavProp@odata.navigationLink\":\"http://example.com/EntitySet(123)/AnotherContainedNavProp\"," +
                                           "\"AnotherContainedNonCollectionNavProp@odata.associationLink\":\"http://example.com/EntitySet(123)/AnotherContainedNonCollectionNavProp/$ref\"," +
                                           "\"AnotherContainedNonCollectionNavProp@odata.navigationLink\":\"http://example.com/EntitySet(123)/AnotherContainedNonCollectionNavProp\"," +
                                           "\"UnknownNonCollectionNavProp@odata.associationLink\":\"http://example.com/EntitySet(123)/UnknownNonCollectionNavProp/$ref\"," +
                                           "\"UnknownNonCollectionNavProp@odata.navigationLink\":\"http://example.com/EntitySet(123)/UnknownNonCollectionNavProp\"," +
                                           "\"UnknownCollectionNavProp@odata.associationLink\":\"http://example.com/EntitySet(123)/UnknownCollectionNavProp/$ref\"," +
                                           "\"UnknownCollectionNavProp@odata.navigationLink\":\"http://example.com/EntitySet(123)/UnknownCollectionNavProp\"," +
                                           "\"EnumAsKeyContainedNavProp@odata.associationLink\":\"http://example.com/EntitySet(123)/EnumAsKeyContainedNavProp/$ref\","+
                                           "\"EnumAsKeyContainedNavProp@odata.navigationLink\":\"http://example.com/EntitySet(123)/EnumAsKeyContainedNavProp\"," +
                                           "\"StreamProp1@odata.mediaEditLink\":\"http://example.com/EntitySet(123)/StreamProp1\"," +
                                           "\"StreamProp1@odata.mediaReadLink\":\"http://example.com/EntitySet(123)/StreamProp1\"," +
                                           "\"StreamProp2@odata.mediaEditLink\":\"http://example.com/EntitySet(123)/StreamProp2\"," +
                                           "\"StreamProp2@odata.mediaReadLink\":\"http://example.com/EntitySet(123)/StreamProp2\"," +
                                           "\"#Namespace.AlwaysBindableAction1\":{\"title\":\"Namespace.AlwaysBindableAction1\",\"target\":\"http://example.com/EntitySet(123)/Namespace.AlwaysBindableAction1\"}," +
                                           "\"#Namespace.AlwaysBindableAction2\":{\"title\":\"Namespace.AlwaysBindableAction2\",\"target\":\"http://example.com/EntitySet(123)/Namespace.AlwaysBindableAction2\"}," +
                                           "\"#Namespace.AlwaysBindableFunction1\":{\"title\":\"Namespace.AlwaysBindableFunction1\",\"target\":\"http://example.com/EntitySet(123)/Namespace.AlwaysBindableFunction1\"}," +
                                           "\"#Namespace.AlwaysBindableFunction2\":{\"title\":\"Namespace.AlwaysBindableFunction2\",\"target\":\"http://example.com/EntitySet(123)/Namespace.AlwaysBindableFunction2\"}," +
                                           "\"#Namespace.Function3\":{\"title\":\"Namespace.Function3\",\"target\":\"http://example.com/EntitySet(123)/Namespace.Function3\"}," +
                                           "\"#Namespace.Function4\":{\"title\":\"Namespace.Function4\",\"target\":\"http://example.com/EntitySet(123)/Namespace.Function4\"}" +
                                           "}";

            Assert.Equal(expectedPayload, GetWriterOutputForEntryWithOnlyData("application/json;odata.metadata=full", true));
        }

        [Fact]
        public void WritingInFullMetadataModeWithProjectionShouldWriteMissingMetadataForEntryWithOnlyDataIfAutoComputePayloadMetadataInJsonIsTrue()
        {
            const string expectedPayload = "{" +
                                           "\"@odata.context\":\"http://example.com/$metadata#EntitySet(StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1)/$entity\"," +
                                           "\"@odata.id\":\"EntitySet(123)\"," +
                                           "\"@odata.etag\":\"W/\\\"'Bob'\\\"\"," +
                                           "\"@odata.editLink\":\"EntitySet(123)\"," +
                                           "\"@odata.mediaEditLink\":\"EntitySet(123)/$value\"," +
                                           "\"ID\":123," +
                                           "\"Name\":\"Bob\"," +
                                           "\"DeferredNavLink@odata.associationLink\":\"http://example.com/EntitySet(123)/DeferredNavLink/$ref\"," +
                                           "\"DeferredNavLink@odata.navigationLink\":\"http://example.com/EntitySet(123)/DeferredNavLink\"," +
                                           "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/EntitySet(123)/ExpandedNavLink/$ref\"," +
                                           "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/EntitySet(123)/ExpandedNavLink\"," +
                                           "\"ExpandedNavLink\":[]," +
                                           "\"StreamProp1@odata.mediaEditLink\":\"http://example.com/EntitySet(123)/StreamProp1\"," +
                                           "\"StreamProp1@odata.mediaReadLink\":\"http://example.com/EntitySet(123)/StreamProp1\"," +
                                           "\"#Namespace.AlwaysBindableAction1\":{\"title\":\"Namespace.AlwaysBindableAction1\",\"target\":\"http://example.com/EntitySet(123)/Namespace.AlwaysBindableAction1\"}," +
                                           "\"#Namespace.AlwaysBindableFunction1\":{\"title\":\"Namespace.AlwaysBindableFunction1\",\"target\":\"http://example.com/EntitySet(123)/Namespace.AlwaysBindableFunction1\"}" +
                                           "}";
            Assert.Equal(expectedPayload,
                GetWriterOutputForEntryWithOnlyData("application/json;odata.metadata=full", true, "StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1"));
        }

        [Fact]
        public void WritingInFullMetadataModeWithExpandAndProjectionShouldWriteMissingMetadataForProjectedPropertiesIfAutoComputePayloadMetadataInJsonIsTrue()
        {
            const string expectedPayload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1,DeferredNavLink,ExpandedNavLink(StreamProp1,Namespace.AlwaysBindableAction1,ExpandedNavLink(StreamProp2,Namespace.AlwaysBindableAction1)))\"," +
                    "\"value\":[" +
                    "{" +
                        "\"@odata.id\":\"EntitySet(123)\"," +
                        "\"@odata.etag\":\"W/\\\"'Bob'\\\"\"," +
                        "\"@odata.editLink\":\"EntitySet(123)\"," +
                        "\"@odata.mediaEditLink\":\"EntitySet(123)/$value\"," +
                        "\"ID\":123," +
                        "\"Name\":\"Bob\"," +
                        "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/expanded/association\"," +
                        "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/expanded/navigation\"," +
                        "\"ExpandedNavLink\":[" +
                        "{" +
                            "\"@odata.id\":\"EntitySet(234)\"," +
                            "\"@odata.etag\":\"W/\\\"'Foo'\\\"\"," +
                            "\"@odata.editLink\":\"EntitySet(234)\"," +
                            "\"@odata.mediaEditLink\":\"EntitySet(234)/$value\"," +
                            "\"ID\":234," +
                            "\"Name\":\"Foo\"," +
                            "\"DeferredNavLink@odata.associationLink\":\"http://example.com/EntitySet(234)/DeferredNavLink/$ref\"," +
                            "\"DeferredNavLink@odata.navigationLink\":\"http://example.com/EntitySet(234)/DeferredNavLink\"," +
                            "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/expanded/association\"," +
                            "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/expanded/navigation\"," +
                            "\"ExpandedNavLink\":[" +
                            "{" +
                                "\"@odata.id\":\"EntitySet(345)\"," +
                                "\"@odata.etag\":\"W/\\\"'Bar'\\\"\"," +
                                "\"@odata.editLink\":\"EntitySet(345)\"," +
                                "\"@odata.mediaEditLink\":\"EntitySet(345)/$value\"," +
                                "\"ID\":345," +
                                "\"Name\":\"Bar\"," +
                                "\"StreamProp2@odata.mediaEditLink\":\"http://example.com/EntitySet(345)/StreamProp2\"," +
                                "\"StreamProp2@odata.mediaReadLink\":\"http://example.com/EntitySet(345)/StreamProp2\"," +
                                "\"#Namespace.AlwaysBindableAction1\":{\"title\":\"Namespace.AlwaysBindableAction1\",\"target\":\"http://example.com/EntitySet(345)/Namespace.AlwaysBindableAction1\"}" +
                            "}]," +
                            "\"StreamProp1@odata.mediaEditLink\":\"http://example.com/EntitySet(234)/StreamProp1\"," +
                            "\"StreamProp1@odata.mediaReadLink\":\"http://example.com/EntitySet(234)/StreamProp1\"," +
                            "\"#Namespace.AlwaysBindableAction1\":{\"title\":\"Namespace.AlwaysBindableAction1\",\"target\":\"http://example.com/EntitySet(234)/Namespace.AlwaysBindableAction1\"}" +
                        "}]," +
                        "\"DeferredNavLink@odata.associationLink\":\"http://example.com/EntitySet(123)/DeferredNavLink/$ref\"," +
                        "\"DeferredNavLink@odata.navigationLink\":\"http://example.com/EntitySet(123)/DeferredNavLink\"," +
                        "\"StreamProp1@odata.mediaEditLink\":\"http://example.com/EntitySet(123)/StreamProp1\"," +
                        "\"StreamProp1@odata.mediaReadLink\":\"http://example.com/EntitySet(123)/StreamProp1\"," +
                        "\"#Namespace.AlwaysBindableAction1\":{\"title\":\"Namespace.AlwaysBindableAction1\",\"target\":\"http://example.com/EntitySet(123)/Namespace.AlwaysBindableAction1\"}," +
                        "\"#Namespace.AlwaysBindableFunction1\":{\"title\":\"Namespace.AlwaysBindableFunction1\",\"target\":\"http://example.com/EntitySet(123)/Namespace.AlwaysBindableFunction1\"}" +
                    "}]" +
                "}";

            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(), this.entryWithOnlyData,
                this.expandedNavLinkWithPayloadMetadata, new ODataResourceSet(), this.entryWithOnlyData2, this.navLinkWithoutPayloadMetadata,
                this.expandedNavLinkWithPayloadMetadata, new ODataResourceSet(), this.entryWithOnlyData3
            };

            const string selectClause = "StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1,DeferredNavLink";
            const string expandClause = "ExpandedNavLink($select=StreamProp1,Namespace.AlwaysBindableAction1;$expand=ExpandedNavLink($select=StreamProp2,Namespace.AlwaysBindableAction1))";
            Assert.Equal(expectedPayload,
                this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, selectClause, expandClause));
        }

        [Fact]
        public void WritingInFullMetadataModeWithExpandTheExpanedODataEntryShouldContainsParentODataEntryMetadataBuilderIfAutoComputePayloadMetadataInJsonIsTrue()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(), this.entryWithOnlyData,
                this.expandedNavLinkWithPayloadMetadata, new ODataResourceSet(), this.entryWithOnlyData2, this.navLinkWithoutPayloadMetadata,
                this.expandedNavLinkWithPayloadMetadata, new ODataResourceSet(), this.entryWithOnlyData3
            };

            const string selectClause = "StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1,DeferredNavLink";
            const string expandClause = "ExpandedNavLink($select=StreamProp1,Namespace.AlwaysBindableAction1;$expand=ExpandedNavLink($select=StreamProp2,Namespace.AlwaysBindableAction1))";
            this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, selectClause, expandClause);

            Assert.Null(this.entryWithOnlyData.MetadataBuilder.ParentMetadataBuilder);
            Assert.Same(this.entryWithOnlyData.MetadataBuilder, this.entryWithOnlyData2.MetadataBuilder.ParentMetadataBuilder);
            Assert.Same(this.entryWithOnlyData2.MetadataBuilder, this.entryWithOnlyData3.MetadataBuilder.ParentMetadataBuilder);
        }

        [Fact]
        public void WritingInFullMetadataModeWithExpandWithCollectionContainedElementIfAutoComputePayloadMetadataInJsonIsTrue()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(), this.entryWithOnlyData,
                this.containedCollectionNavLinkWithPayloadMetadata, new ODataResourceSet(), this.entryWithOnlyData2
            };

            const string selectClause = "ContainedNavProp";
            const string expandClause = "ExpandedNavLink($select=ContainedNavProp)";
            this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, selectClause, expandClause, "EntitySet(123)");

            Uri containedId = new Uri("http://example.com/EntitySet(123)/ContainedNavProp(234)");
            Assert.Equal(containedId, this.entryWithOnlyData2.Id);
        }

        [Fact]
        public void WritingInFullMetadataModeWithTopLevelContainedEntity()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                this.entryWithOnlyData
            };

            IEdmNavigationProperty containedNavProp = EntityType.FindProperty("ContainedNavProp") as IEdmNavigationProperty;
            IEdmEntitySetBase contianedEntitySet = EntitySet.FindNavigationTarget(containedNavProp) as IEdmEntitySetBase;
            string resourcePath = "EntitySet(123)/ContainedNavProp(123)";
            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, contianedEntitySet, EntityType, null, null, resourcePath);

            Uri containedId = new Uri("http://example.com/EntitySet(123)/ContainedNavProp(123)");
            Assert.Equal(containedId, this.entryWithOnlyData.Id);

            string expectedContextUriString = "$metadata#EntitySet(123)/ContainedNavProp/$entity";
            Assert.Contains(expectedContextUriString, result);
        }

        [Fact]
        public void WritingInFullMetadataModeWithTopLevelContainedEntityWithEnumAsKey()
        {
            ODataResource entry = new ODataResource { Properties = new[] { new ODataProperty { Name = "GenderID", Value = new ODataEnumValue("Male", Gender.FullTypeName()) } } };
            ODataItem[] itemsToWrite = { entry };

            IEdmNavigationProperty containedNavProp = EntityType.FindProperty("EnumAsKeyContainedNavProp") as IEdmNavigationProperty;
            IEdmEntitySetBase contianedEntitySet = EntitySet.FindNavigationTarget(containedNavProp) as IEdmEntitySetBase;
            string resourcePath = "EntitySet(123)/EnumAsKeyContainedNavProp(Namespace.Gender'Male')";
            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, contianedEntitySet, EnumAsKeyEntityType, null, null, resourcePath);

            Uri containedId = new Uri("http://example.com/EntitySet(123)/EnumAsKeyContainedNavProp(Namespace.Gender'Male')");
            Assert.Equal(containedId, entry.Id);

            string expectedContextUriString = "$metadata#EntitySet(123)/EnumAsKeyContainedNavProp/$entity";
            Assert.Contains(expectedContextUriString, result);
        }

        [Fact]
        public void WritingInFullMetadataModeWithTopLevelContainedEntityWithFunctionUriPath()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                this.entryWithOnlyData
            };

            IEdmNavigationProperty containedNavProp = EntityType.FindProperty("ContainedNonCollectionNavProp") as IEdmNavigationProperty;
            IEdmEntitySetBase contianedEntitySet = EntitySet.FindNavigationTarget(containedNavProp) as IEdmEntitySetBase;
            string resourcePath = "EntitySet(123)/Namespace.Function3";
            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, contianedEntitySet, EntityType, null, null, resourcePath);

            string expectedContextUriString = "$metadata#EntitySet(123)/ContainedNonCollectionNavProp";
            Assert.Contains(expectedContextUriString, result);
        }

        [Fact]
        public void WritingInFullMetadataModeWithTopLevelNonContainedEntityWithFunctionUriPath()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                this.entryWithOnlyData
            };
            string resourcePath = "EntitySet(123)/Namespace.Function4";
            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, null, null, resourcePath);

            string expectedContextUriString = "$metadata#EntitySet/$entity";
            Assert.Contains(expectedContextUriString, result);
        }

        [Fact]
        public void WritingInFullMetadataModeWithTopLevelContainedEntryWithTypeCast()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                this.entryWithOnlyData
            };

            IEdmNavigationProperty containedDerivedNavProp = EntityType.FindProperty("ContainedNavProp") as IEdmNavigationProperty;
            IEdmEntitySetBase contianedEntitySet = EntitySet.FindNavigationTarget(containedDerivedNavProp) as IEdmEntitySetBase;
            string resourcePath = "EntitySet(123)/ContainedNavProp(123)/Namespace.DerivedType/";
            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, contianedEntitySet, DerivedType, null, null, resourcePath);

            Uri containedId = new Uri("http://example.com/EntitySet(123)/ContainedNavProp(123)");
            Assert.Equal(containedId, this.entryWithOnlyData.Id);

            string expectedContextUriString = "$metadata#EntitySet(123)/ContainedNavProp/Namespace.DerivedType/$entity";
            Assert.Contains(expectedContextUriString, result);
        }

        [Fact]
        public void WritingInFullMetadataModeWithTopLevelContainedFeedWithTypeCast()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(), this.entryWithOnlyData, this.entryWithOnlyData2
            };

            IEdmNavigationProperty containedDerivedNavProp = EntityType.FindProperty("ContainedNavProp") as IEdmNavigationProperty;
            IEdmEntitySetBase contianedEntitySet = EntitySet.FindNavigationTarget(containedDerivedNavProp) as IEdmEntitySetBase;
            string resourcePath = "EntitySet(123)/ContainedNavProp/Namespace.DerivedType/";
            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, contianedEntitySet, DerivedType, null, null, resourcePath);

            string expectedContextUriString = "$metadata#EntitySet(123)/ContainedNavProp/Namespace.DerivedType\"";
            Assert.Contains(expectedContextUriString, result);
        }

        [Fact]
        public void WritingInFullMetadataModeWithTopLevelContainedEntityInFeed()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(), this.entryWithOnlyData
            };

            IEdmNavigationProperty containedNavProp = EntityType.FindProperty("ContainedNavProp") as IEdmNavigationProperty;
            IEdmEntitySetBase contianedEntitySet = EntitySet.FindNavigationTarget(containedNavProp) as IEdmEntitySetBase;
            string resourcePath = "EntitySet(123)/ContainedNavProp";
            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, contianedEntitySet, EntityType, null, null, resourcePath);

            Uri containedId = new Uri("http://example.com/EntitySet(123)/ContainedNavProp(123)");
            Assert.Equal(containedId, this.entryWithOnlyData.Id);

            string expectedContextUriString = "$metadata#EntitySet(123)/ContainedNavProp";
            Assert.Contains(expectedContextUriString, result);
        }

        [Fact]
        public void WritingInFullMetadataModeWithExpandWithContainedElementIfAutoComputePayloadMetadataInJsonIsTrue()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(), this.entryWithOnlyData,
                this.containedNavLinkWithPayloadMetadata, this.entryWithOnlyData2,
                this.containedNavLinkWithPayloadMetadata, this.entryWithOnlyData3
            };

            const string selectClause = "ContainedNonCollectionNavProp";
            const string expandClause = "ExpandedNavLink($select=ContainedNonCollectionNavProp;$expand=ExpandedNavLink($select=ContainedNonCollectionNavProp))";
            this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, selectClause, expandClause, "EntitySet");

            Uri containedIdLevel1 = new Uri("http://example.com/EntitySet(123)/ContainedNonCollectionNavProp");
            Assert.Equal(containedIdLevel1, this.entryWithOnlyData2.Id);
            Uri containedIdLevel2 = new Uri("http://example.com/EntitySet(123)/ContainedNonCollectionNavProp/ContainedNonCollectionNavProp");
            Assert.Equal(containedIdLevel2, this.entryWithOnlyData3.Id);
        }

        [Fact]
        public void WritingInFullMetadataModeWithContainedEntityInBaseEntityInFeed()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(),
                this.derivedEntry,
                this.containedNavLinkWithPayloadMetadata,
                this.entryWithOnlyData
            };

            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, null, null, "EntitySet");

            Uri containedId = new Uri("http://example.com/EntitySet(345)/ContainedNonCollectionNavProp");
            Assert.Equal(containedId, this.entryWithOnlyData.Id);
        }

        [Fact]
        public void WritingInFullMetadataModeWithDirectyAccessContainedEntityInFeed()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(),
                this.derivedEntry,
            };

            var navProp = EntityType.FindProperty("ContainedNonCollectionNavProp") as IEdmNavigationProperty;
            var containedEntitySet = EntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;

            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, containedEntitySet, DerivedType, null, null, "EntitySet(0)/Namespace.DerivedType/ContainedNonCollectionNavProp");

            Uri containedId = new Uri("http://example.com/EntitySet(0)/Namespace.DerivedType/ContainedNonCollectionNavProp");
            Assert.Equal(containedId, this.derivedEntry.Id);
        }

        [Fact]
        public void WritingInFullMetadataModeWithDirectyAccessDerivedContainedEntityInFeed()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(),
                this.derivedEntry,
            };

            var navProp = DerivedType.FindProperty("DerivedContainedNavProp") as IEdmNavigationProperty;
            var containedEntitySet = EntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;

            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, containedEntitySet, DerivedType, null, null, "EntitySet(0)/Namespace.DerivedType/DerivedContainedNavProp");

            Uri containedId = new Uri("http://example.com/EntitySet(0)/Namespace.DerivedType/DerivedContainedNavProp(345)");
            Assert.Equal(containedId, this.derivedEntry.Id);
        }

        [Fact]
        public void WritingInFullMetadataModeWithContainedEntityInDerivedEntityInFeed()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(),
                this.derivedEntry,
                this.derivedContainedCollectionNavLinkWithPayloadMetadata,
                new ODataResourceSet(),
                this.entryWithOnlyData
            };

            string result = this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, null, null, "EntitySet");

            Uri containedId = new Uri("http://example.com/EntitySet(345)/Namespace.DerivedType/DerivedContainedNavProp(123)");
            Assert.Equal(containedId, this.entryWithOnlyData.Id);
        }

        [Fact]
        public void WritingInFullMetadataModeWithExpandWithContainedElementShouldThrowExceptionIfODataPathIsNotSet()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(), this.entryWithOnlyData,
                this.containedNavLinkWithPayloadMetadata, this.entryWithOnlyData2,
                this.containedNavLinkWithPayloadMetadata, this.entryWithOnlyData3
            };

            const string selectClause = "ContainedNonCollectionNavProp";
            const string expandClause = "ExpandedNavLink($select=ContainedNonCollectionNavProp;$expand=ExpandedNavLink($select=ContainedNonCollectionNavProp))";

            Action test = () => this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, selectClause, expandClause);
            test.Throws<ODataException>(Strings.ODataWriterCore_PathInODataUriMustBeSetWhenWritingContainedElement);
        }

        [Fact]
        public void WritingInFullMetadataModeWithExpandWithContainedElementShouldThrowExceptionIfEntryKeyIsNotSet()
        {
            var entryWithoutKey = new ODataResource { Properties = new[] { new ODataProperty { Name = "Name", Value = "IHaveNoKey" } }, };
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(), entryWithoutKey,
                this.containedNavLinkWithPayloadMetadata, this.entryWithOnlyData2,
                this.containedNavLinkWithPayloadMetadata, this.entryWithOnlyData3
            };

            const string selectClause = "ContainedNonCollectionNavProp";
            const string expandClause = "ExpandedNavLink($select=ContainedNonCollectionNavProp;$expand=ExpandedNavLink($select=ContainedNonCollectionNavProp))";

            Action test = () => this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, selectClause, expandClause);
            test.Throws<ODataException>(Strings.EdmValueUtils_PropertyDoesntExist("Namespace.EntityType", "ID"));
        }

        [Fact]
        public void WritingInFullMetadataModeForNavigationPropertyWithoutBindingShouldThrowODataResourceTypeContext_MetadataOrSerializationInfoMissingException()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(), this.entryWithOnlyData,
                this.unknownCollectionNavLinkWithPayloadMetadata, new ODataResourceSet(), this.entryWithOnlyData2
            };

            const string selectClause = "UnknownCollectionNavProp";
            const string expandClause = "ExpandedNavLink($expand=UnknownCollectionNavProp)";

            Action test = () => this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, selectClause, expandClause);
            test.Throws<ODataException>(Strings.ODataMetadataBuilder_UnknownEntitySet("UnknownCollectionNavProp"));
        }

        [Fact]
        public void WritingInFullMetadataModeForNavigationPropertyWithoutBindingShouldPassIfSerializationInfoHaveBeenSetOnTheEntry()
        {
            ODataItem[] itemsToWrite = new ODataItem[]
            {
                new ODataResourceSet(), this.entryWithOnlyData,
                this.unknownCollectionNavLinkWithPayloadMetadata, new ODataResourceSet(), this.entryWithOnlyData2
            };

            this.entryWithOnlyData2.TypeName = EntityType.FullName();
            this.entryWithOnlyData2.MediaResource = new ODataStreamReferenceValue();
            this.entryWithOnlyData2.Properties.First(p => p.Name == "ID").SetSerializationInfo(new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.Key });
            this.entryWithOnlyData2.Properties.First(p => p.Name == "Name").SetSerializationInfo(new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.ETag });
            this.entryWithOnlyData2.SerializationInfo = new ODataResourceSerializationInfo()
            {
                NavigationSourceKind = EdmNavigationSourceKind.EntitySet,
                ExpectedTypeName = EntityType.FullName(),
                NavigationSourceEntityTypeName = EntitySet.EntityType().FullName(),
                IsFromCollection = true,
                NavigationSourceName = EntitySet.Name
            };

            const string selectClause = "UnknownCollectionNavProp";
            const string expandClause = "ExpandedNavLink($expand=UnknownCollectionNavProp)";

            this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, Model, EntitySet, EntityType, selectClause, expandClause);
            Assert.Equal(new Uri("http://example.com/EntitySet(234)"), entryWithOnlyData2.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithContainedElementShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(ContainedNavProp2,ExpandedNavLink,ExpandedNavLink(ContainedNavProp2))\"," +
                    "\"value\":[" +
                    "{" +
                        "\"ContainedNonCollectionNavProp@odata.context\":\"http://example.com/$metadata#EntitySet(123)/ContainedNonCollectionNavProp/$entity\"," +
                        "\"ContainedNonCollectionNavProp\":" +
                        "{" +
                            "\"ID\": 234," +
                            "\"Name\":\"Foo\"" +
                        "}," +
                    "\"ID\" : 123," +
                    "\"Name\" : \"Bob\"" +
                "} ] }";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=mini");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var reader = messageReader.CreateODataResourceSetReader(EntitySet, EntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(123)/ContainedNonCollectionNavProp");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithContainedEntitySetShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(1)/ContainedNavProp(ID)\"," +
                    "\"value\":[" +
                    "{" +
                    "\"ID\" : 123" +
                    "}" +
                "] }";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var navProp = EntityType.FindProperty("ContainedNavProp") as IEdmNavigationProperty;
                var containedEntitySet = EntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;
                var reader = messageReader.CreateODataResourceSetReader(containedEntitySet, EntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(1)/ContainedNavProp(123)");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithContainedEntitySetOfAnotherTypeShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(1)/AnotherContainedNavProp\"," +
                    "\"value\":[" +
                    "{" +
                    "\"ID\" : 123," +
                    "\"Name\" : \"Bob\"" +
                    "}" +
                "] }";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var navProp = EntityType.FindProperty("AnotherContainedNavProp") as IEdmNavigationProperty;
                var containedEntitySet = EntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;
                var reader = messageReader.CreateODataResourceSetReader(containedEntitySet, AnotherEntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(1)/AnotherContainedNavProp(123)");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeUseDefaultCtorWithContainedEntitySetOfAnotherTypeShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(1)/AnotherContainedNavProp\"," +
                    "\"value\":[" +
                    "{" +
                    "\"ID\" : 123," +
                    "\"Name\" : \"Bob\"" +
                    "}" +
                "] }";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var reader = messageReader.CreateODataResourceSetReader();
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(1)/AnotherContainedNavProp(123)");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithContainedNonCollectionEntitySetShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(1)/ContainedNonCollectionNavProp/$entity\"," +
                    "\"ID\" : 123," +
                    "\"Name\" : \"Bob\"" +
                "}";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var navProp = EntityType.FindProperty("ContainedNonCollectionNavProp") as IEdmNavigationProperty;
                var containedEntitySet = EntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;
                var reader = messageReader.CreateODataResourceReader(containedEntitySet, EntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(1)/ContainedNonCollectionNavProp");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithContainedNonCollectionEntitySetOfAnotherTypeShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(1)/AnotherContainedNonCollectionNavProp/$entity\"," +
                    "\"ID\" : 123," +
                    "\"Name\" : \"Bob\"" +
                "}";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var navProp = EntityType.FindProperty("AnotherContainedNonCollectionNavProp") as IEdmNavigationProperty;
                var containedEntitySet = EntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;
                var reader = messageReader.CreateODataResourceReader(containedEntitySet, AnotherEntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(1)/AnotherContainedNonCollectionNavProp");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithTwoLevelContainedEntitySetShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(1)/ContainedNavProp(2)/ContainedNavProp\"," +
                    "\"value\":[" +
                    "{" +
                    "\"ID\" : 123," +
                    "\"Name\" : \"Bob\"" +
                    "}" +
                "] }";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var navProp = EntityType.FindProperty("ContainedNavProp") as IEdmNavigationProperty;
                var containedEntitySet = EntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;
                containedEntitySet = containedEntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;
                var reader = messageReader.CreateODataResourceSetReader(containedEntitySet, EntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(1)/ContainedNavProp(2)/ContainedNavProp(123)");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithTwoLevelContainedEntitySetOfAnotherTypeShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(1)/ContainedNavProp(2)/AnotherContainedNavProp\"," +
                    "\"value\":[" +
                    "{" +
                    "\"ID\" : 123," +
                    "\"Name\" : \"Bob\"" +
                    "}" +
                "] }";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var navProp = EntityType.FindProperty("ContainedNavProp") as IEdmNavigationProperty;
                var containedEntitySet = EntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;
                var anotherNavProp = EntityType.FindProperty("AnotherContainedNavProp") as IEdmNavigationProperty;
                containedEntitySet = containedEntitySet.FindNavigationTarget(anotherNavProp) as IEdmEntitySetBase;
                var reader = messageReader.CreateODataResourceSetReader(containedEntitySet, AnotherEntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(1)/ContainedNavProp(2)/AnotherContainedNavProp(123)");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithContainedEntitySetAndTypeCastShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(1)/ContainedNavProp/Namespace.EntityType\"," +
                    "\"value\":[" +
                    "{" +
                    "\"ID\" : 123," +
                    "\"Name\" : \"Bob\"" +
                    "}" +
                "] }";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var navProp = EntityType.FindProperty("ContainedNavProp") as IEdmNavigationProperty;
                var containedEntitySet = EntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;
                var reader = messageReader.CreateODataResourceSetReader(containedEntitySet, EntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(1)/ContainedNavProp(123)");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithNonCollectionContainedEntitySetAndTypeCastShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(1)/ContainedNonCollectionNavProp/Namespace.EntityType(ID)/$entity\"," +
                    "\"ID\" : 123" +
                "}";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var navProp = EntityType.FindProperty("ContainedNonCollectionNavProp") as IEdmNavigationProperty;
                var containedEntitySet = EntitySet.FindNavigationTarget(navProp) as IEdmEntitySetBase;
                var reader = messageReader.CreateODataResourceReader(containedEntitySet, EntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(1)/ContainedNonCollectionNavProp");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithExpandedCollectionContainedEntitySetAndTypeCastShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(ContainedNavProp/Namespace.EntityType)/$entity\"," +
                    "\"ID\" : 123," +
                    "\"Name\" : \"Bob\"," +
                    "\"ContainedNavProp@odata.context\":\"http://example.com/$metadata#EntitySet(123)/ContainedNavProp/Namespace.EntityType\"," +
                    "\"ContainedNavProp\":" +
                        "[{" +
                            "\"ID\" : 234," +
                            "\"Name\" : \"Foo\"" +
                        "}]" +
                "}";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var reader = messageReader.CreateODataResourceReader(EntitySet, EntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(123)/ContainedNavProp(234)");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void ReadingInMinialMetadataModeWithExpandedNonCollectionContainedEntitySetAndTypeCastShouldBeAbleToGenerateId()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(ContainedNonCollectionNavProp/Namespace.EntityType)/$entity\"," +
                    "\"ID\" : 123," +
                    "\"Name\" : \"Bob\"," +
                    "\"ContainedNonCollectionNavProp@odata.context\":\"http://example.com/$metadata#EntitySet(123)/ContainedNonCollectionNavProp/Namespace.EntityType/$entity\"," +
                    "\"ContainedNonCollectionNavProp\":" +
                        "{" +
                            "\"ID\" : 234," +
                            "\"Name\" : \"Foo\"" +
                        "}" +
                "}";

            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            ODataResource topLevelEntry = null;
            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var reader = messageReader.CreateODataResourceReader(EntitySet, EntityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            topLevelEntry = (ODataResource)reader.Item;
                            entryList.Add(topLevelEntry);
                            break;
                    }
                }
            }

            Uri containedId = new Uri("http://example.com/EntitySet(123)/ContainedNonCollectionNavProp");

            ODataResource containedEntry = entryList[0];
            Assert.Equal(containedId, containedEntry.Id);
        }

        [Fact]
        public void WritingInFullMetadataModeWithExpandAndProjectionWithMissingStreamAndActionAndFunctionWhenAutoComputePayloadMetadataInJsonIsTrue()
        {
            const string expectedPayload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1,DeferredNavLink,ExpandedNavLink(StreamProp1,Namespace.AlwaysBindableAction1,ExpandedNavLink(StreamProp2,Namespace.AlwaysBindableAction1)))\"," +
                    "\"value\":[" +
                    "{" +
                        "\"@odata.type\":\"#Namespace.EntityType\"," +
                        "\"@odata.id\":\"EntitySet(123)\"," +
                        "\"@odata.etag\":\"W/\\\"'Bob'\\\"\"," +
                        "\"@odata.editLink\":\"EntitySet(123)\"," +
                        "\"@odata.mediaEditLink\":\"EntitySet(123)/$value\"," +
                        "\"ID\":123," +
                        "\"Name\":\"Bob\"," +
                        "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/expanded/association\"," +
                        "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/expanded/navigation\"," +
                        "\"ExpandedNavLink@odata.type\":\"#Collection(Namespace.EntityType)\"," +
                        "\"ExpandedNavLink\":[" +
                        "{" +
                            "\"@odata.type\":\"#Namespace.EntityType\"," +
                            "\"@odata.id\":\"EntitySet(234)\"," +
                            "\"@odata.etag\":\"W/\\\"'Foo'\\\"\"," +
                            "\"@odata.editLink\":\"EntitySet(234)\"," +
                            "\"@odata.mediaEditLink\":\"EntitySet(234)/$value\"," +
                            "\"ID\":234," +
                            "\"Name\":\"Foo\"," +
                            "\"DeferredNavLink@odata.associationLink\":\"http://example.com/EntitySet(234)/DeferredNavLink/$ref\"," +
                            "\"DeferredNavLink@odata.navigationLink\":\"http://example.com/EntitySet(234)/DeferredNavLink\"," +
                            "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/EntitySet(234)/ExpandedNavLink/$ref\"," +
                            "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/EntitySet(234)/ExpandedNavLink\"," +
                            "\"ExpandedNavLink@odata.type\":\"#Collection(Namespace.EntityType)\"," +
                            "\"ExpandedNavLink\":[" +
                            "{" +
                                "\"@odata.type\":\"#Namespace.EntityType\"," +
                                "\"@odata.id\":\"EntitySet(345)\"," +
                                "\"@odata.etag\":\"W/\\\"'Bar'\\\"\"," +
                                "\"@odata.editLink\":\"EntitySet(345)\"," +
                                "\"@odata.mediaEditLink\":\"EntitySet(345)/$value\"," +
                                "\"ID\":345," +
                                "\"Name\":\"Bar\"" +
                            "}]" +
                        "}]" +
                    "}]" +
                "}";

            ODataResourceSerializationInfo serializationInfo = new ODataResourceSerializationInfo { NavigationSourceName = EntitySet.Name, NavigationSourceEntityTypeName = EntityType.FullName(), ExpectedTypeName = EntityType.FullName() };

            var feed = new ODataResourceSet();
            feed.SetSerializationInfo(serializationInfo);
            this.entryWithOnlyData.TypeName = EntityType.FullName();
            this.entryWithOnlyData.MediaResource = new ODataStreamReferenceValue();
            this.entryWithOnlyData.Properties.First(p => p.Name == "ID").SetSerializationInfo(new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.Key });
            this.entryWithOnlyData.Properties.First(p => p.Name == "Name").SetSerializationInfo(new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.ETag });
            this.entryWithOnlyData2.TypeName = EntityType.FullName();
            this.entryWithOnlyData2.MediaResource = new ODataStreamReferenceValue();
            this.entryWithOnlyData2.Properties.First(p => p.Name == "ID").SetSerializationInfo(new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.Key });
            this.entryWithOnlyData2.Properties.First(p => p.Name == "Name").SetSerializationInfo(new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.ETag });
            this.entryWithOnlyData3.TypeName = EntityType.FullName();
            this.entryWithOnlyData3.MediaResource = new ODataStreamReferenceValue();
            this.entryWithOnlyData3.Properties.First(p => p.Name == "ID").SetSerializationInfo(new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.Key });
            this.entryWithOnlyData3.Properties.First(p => p.Name == "Name").SetSerializationInfo(new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.ETag });

            ODataItem[] itemsToWrite = new ODataItem[]
            {
                feed, this.entryWithOnlyData,
                this.expandedNavLinkWithPayloadMetadata, feed, this.entryWithOnlyData2, this.navLinkWithoutPayloadMetadata,
                this.expandedNavLinkWithoutPayloadMetadata, feed, this.entryWithOnlyData3
            };

            const string selectClause = "StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1,DeferredNavLink";
            const string expandClause = "ExpandedNavLink($select=StreamProp1,Namespace.AlwaysBindableAction1;$expand=ExpandedNavLink($select=StreamProp2,Namespace.AlwaysBindableAction1))";
            Assert.Equal(expectedPayload,
                this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, edmModel: Model, edmEntitySet: null, edmEntityType: EntityType, selectClause: selectClause, expandClause: expandClause));
        }

        [Fact]
        public void WritingInFullMetadataModeWithExpandAndProjectionWithModelWhenAutoComputePayloadMetadataInJsonIsTrue()
        {
            const string expectedPayload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1,DeferredNavLink,ExpandedNavLink(StreamProp1,Namespace.AlwaysBindableAction1,ExpandedNavLink(StreamProp2,Namespace.AlwaysBindableAction1)))\"," +
                    "\"value\":[" +
                    "{" +
                        "\"@odata.type\":\"#Namespace.EntityType\"," +
                        "\"@odata.id\":\"EntitySet(123)\"," +
                        "\"@odata.etag\":\"W/\\\"'Bob'\\\"\"," +
                        "\"@odata.editLink\":\"EntitySet(123)\"," +
                        "\"@odata.mediaEditLink\":\"EntitySet(123)/$value\"," +
                        "\"ID\":123," +
                        "\"Name\":\"Bob\"," +
                        "\"StreamProp1@odata.mediaEditLink\":\"http://example.com/EntitySet(123)/StreamProp1\"," +
                        "\"StreamProp1@odata.mediaReadLink\":\"http://example.com/EntitySet(123)/StreamProp1\"," +
                        "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/expanded/association\"," +
                        "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/expanded/navigation\"," +
                        "\"ExpandedNavLink@odata.type\":\"#Collection(Namespace.EntityType)\"," +
                        "\"ExpandedNavLink\":[" +
                        "{" +
                            "\"@odata.type\":\"#Namespace.EntityType\"," +
                            "\"@odata.id\":\"EntitySet(234)\"," +
                            "\"@odata.etag\":\"W/\\\"'Foo'\\\"\"," +
                            "\"@odata.editLink\":\"EntitySet(234)\"," +
                            "\"@odata.mediaEditLink\":\"EntitySet(234)/$value\"," +
                            "\"ID\":234," +
                            "\"Name\":\"Foo\"," +
                            "\"StreamProp1@odata.mediaEditLink\":\"http://example.com/EntitySet(234)/StreamProp1\"," +
                            "\"StreamProp1@odata.mediaReadLink\":\"http://example.com/EntitySet(234)/StreamProp1\"," +
                            "\"DeferredNavLink@odata.associationLink\":\"http://example.com/EntitySet(234)/DeferredNavLink/$ref\"," +
                            "\"DeferredNavLink@odata.navigationLink\":\"http://example.com/EntitySet(234)/DeferredNavLink\"," +
                            "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/expanded/association\"," +
                            "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/expanded/navigation\"," +
                            "\"ExpandedNavLink@odata.type\":\"#Collection(Namespace.EntityType)\"," +
                            "\"ExpandedNavLink\":[" +
                            "{" +
                                "\"@odata.type\":\"#Namespace.EntityType\"," +
                                "\"@odata.id\":\"EntitySet(345)\"," +
                                "\"@odata.etag\":\"W/\\\"'Bar'\\\"\"," +
                                "\"@odata.editLink\":\"EntitySet(345)\"," +
                                "\"@odata.mediaEditLink\":\"EntitySet(345)/$value\"," +
                                "\"ID\":345," +
                                "\"Name\":\"Bar\"," +
                                "\"StreamProp2@odata.mediaEditLink\":\"http://example.com/EntitySet(345)/StreamProp2\"," +
                                "\"StreamProp2@odata.mediaReadLink\":\"http://example.com/EntitySet(345)/StreamProp2\"," +
                                "\"#Container.AlwaysBindableAction1\":{\"title\":\"Container.AlwaysBindableAction1\",\"target\":\"http://example.com/EntitySet(345)/Container.AlwaysBindableAction1\"}" +
                            "}]," +
                            "\"#Container.AlwaysBindableAction1\":{\"title\":\"Container.AlwaysBindableAction1\",\"target\":\"http://example.com/EntitySet(234)/Container.AlwaysBindableAction1\"}" +
                        "}]," +
                        "\"#Container.AlwaysBindableAction1\":{\"title\":\"Container.AlwaysBindableAction1\",\"target\":\"http://example.com/EntitySet(123)/Container.AlwaysBindableAction1\"}," +
                        "\"#Container.AlwaysBindableFunction1\":{\"title\":\"Container.AlwaysBindableFunction1\",\"target\":\"http://example.com/EntitySet(123)/Container.AlwaysBindableFunction1\"}" +
                    "}]" +
                "}";

            ODataResourceSerializationInfo serializationInfo = new ODataResourceSerializationInfo { NavigationSourceName = EntitySet.Name, NavigationSourceEntityTypeName = EntityType.FullName(), ExpectedTypeName = EntityType.FullName() };

            var feed = new ODataResourceSet();
            feed.SetSerializationInfo(serializationInfo);

            this.entryWithOnlyData = new ODataResource
            {
                TypeName = EntityType.FullName(),
                MediaResource = new ODataStreamReferenceValue(),
                Properties = new[]
                {
                    new ODataProperty { Name = "ID", Value = 123, SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.Key }},
                    new ODataProperty { Name = "Name", Value = "Bob", SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.ETag }},
                    new ODataProperty { Name = "StreamProp1", Value = new ODataStreamReferenceValue() }
                },
            };
            this.entryWithOnlyData.AddAction(new ODataAction { Metadata = new Uri("http://example.com/$metadata#Container.AlwaysBindableAction1") });
            this.entryWithOnlyData.AddFunction(new ODataFunction { Metadata = new Uri("#Container.AlwaysBindableFunction1", UriKind.Relative) });

            this.entryWithOnlyData2 = new ODataResource
            {
                TypeName = EntityType.FullName(),
                MediaResource = new ODataStreamReferenceValue(),
                Properties = new[]
                {
                    new ODataProperty { Name = "ID", Value = 234, SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.Key }},
                    new ODataProperty { Name = "Name", Value = "Foo", SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.ETag }},
                    new ODataProperty { Name = "StreamProp1", Value = new ODataStreamReferenceValue() }
                },
            };
            this.entryWithOnlyData2.AddAction(new ODataAction { Metadata = new Uri("http://example.com/$metadata#Container.AlwaysBindableAction1") });

            this.entryWithOnlyData3 = new ODataResource
            {
                TypeName = EntityType.FullName(),
                MediaResource = new ODataStreamReferenceValue(),
                Properties = new[]
                {
                    new ODataProperty { Name = "ID", Value = 345, SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.Key }},
                    new ODataProperty { Name = "Name", Value = "Bar", SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.ETag }},
                    new ODataProperty { Name = "StreamProp2", Value = new ODataStreamReferenceValue() }
                },
            };
            this.entryWithOnlyData3.AddAction(new ODataAction { Metadata = new Uri("http://example.com/$metadata#Container.AlwaysBindableAction1") });

            ODataItem[] itemsToWrite = new ODataItem[]
            {
                feed, this.entryWithOnlyData,
                this.expandedNavLinkWithPayloadMetadata, feed, this.entryWithOnlyData2, this.navLinkWithoutPayloadMetadata,
                this.expandedNavLinkWithPayloadMetadata, feed, this.entryWithOnlyData3
            };

            const string selectClause = "StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1,DeferredNavLink";
            const string expandClause = "ExpandedNavLink($select=StreamProp1,Namespace.AlwaysBindableAction1;$expand=ExpandedNavLink($select=StreamProp2,Namespace.AlwaysBindableAction1))";
            Assert.Equal(expectedPayload,
                this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=full", true, itemsToWrite, edmModel: Model, edmEntitySet: null, edmEntityType: EntityType, selectClause: selectClause, expandClause: expandClause));
        }

        [Fact]
        public void WritingInMinimalMetadataModeWithExpandAndProjectionWithModelWhenAutoComputePayloadMetadataInJsonIsTrue()
        {
            const string expectedPayload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet(StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1,DeferredNavLink,ExpandedNavLink(StreamProp1,Namespace.AlwaysBindableAction1,ExpandedNavLink(StreamProp2,Namespace.AlwaysBindableAction1)))\"," +
                    "\"value\":[" +
                    "{" +
                        "\"ID\":123," +
                        "\"Name\":\"Bob\"," +
                        "\"ExpandedNavLink@odata.associationLink\":\"http://example.com/expanded/association\"," +
                        "\"ExpandedNavLink@odata.navigationLink\":\"http://example.com/expanded/navigation\"," +
                        "\"ExpandedNavLink\":[" +
                        "{" +
                            "\"ID\":234," +
                            "\"Name\":\"Foo\"," +
                            "\"ExpandedNavLink\":[" +
                            "{" +
                                "\"ID\":345," +
                                "\"Name\":\"Bar\"," +
                                "\"#Container.AlwaysBindableAction1\":{}" +
                            "}]," +
                            "\"#Container.AlwaysBindableAction1\":{}" +
                        "}]," +
                        "\"#Container.AlwaysBindableAction1\":{}," +
                        "\"#Container.AlwaysBindableFunction1\":{}" +
                    "}]" +
                "}";

            ODataResourceSerializationInfo serializationInfo = new ODataResourceSerializationInfo { NavigationSourceName = EntitySet.Name, NavigationSourceEntityTypeName = EntityType.FullName(), ExpectedTypeName = EntityType.FullName() };

            var feed = new ODataResourceSet();
            feed.SetSerializationInfo(serializationInfo);

            this.entryWithOnlyData = new ODataResource
            {
                TypeName = EntityType.FullName(),
                MediaResource = new ODataStreamReferenceValue(),
                Properties = new[]
                {
                    new ODataProperty { Name = "ID", Value = 123, SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.Key }},
                    new ODataProperty { Name = "Name", Value = "Bob", SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.ETag }},
                    new ODataProperty { Name = "StreamProp1", Value = new ODataStreamReferenceValue() }
                },
            };
            this.entryWithOnlyData.AddAction(new ODataAction { Metadata = new Uri("http://example.com/$metadata#Container.AlwaysBindableAction1") });
            this.entryWithOnlyData.AddFunction(new ODataFunction { Metadata = new Uri("#Container.AlwaysBindableFunction1", UriKind.Relative) });

            this.entryWithOnlyData2 = new ODataResource
            {
                TypeName = EntityType.FullName(),
                MediaResource = new ODataStreamReferenceValue(),
                Properties = new[]
                {
                    new ODataProperty { Name = "ID", Value = 234, SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.Key }},
                    new ODataProperty { Name = "Name", Value = "Foo", SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.ETag }},
                    new ODataProperty { Name = "StreamProp1", Value = new ODataStreamReferenceValue() }
                },
            };
            this.entryWithOnlyData2.AddAction(new ODataAction { Metadata = new Uri("http://example.com/$metadata#Container.AlwaysBindableAction1") });

            this.entryWithOnlyData3 = new ODataResource
            {
                TypeName = EntityType.FullName(),
                MediaResource = new ODataStreamReferenceValue(),
                Properties = new[]
                {
                    new ODataProperty { Name = "ID", Value = 345, SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.Key }},
                    new ODataProperty { Name = "Name", Value = "Bar", SerializationInfo = new ODataPropertySerializationInfo { PropertyKind = ODataPropertyKind.ETag }},
                    new ODataProperty { Name = "StreamProp2", Value = new ODataStreamReferenceValue() }
                },
            };
            this.entryWithOnlyData3.AddAction(new ODataAction { Metadata = new Uri("http://example.com/$metadata#Container.AlwaysBindableAction1") });

            ODataItem[] itemsToWrite = new ODataItem[]
            {
                feed, this.entryWithOnlyData,
                this.expandedNavLinkWithPayloadMetadata, feed, this.entryWithOnlyData2, this.navLinkWithoutPayloadMetadata,
                this.expandedNavLinkWithoutPayloadMetadata, feed, this.entryWithOnlyData3
            };

            const string selectClause = "StreamProp1,Namespace.AlwaysBindableAction1,Namespace.AlwaysBindableFunction1,DeferredNavLink";
            const string expandClause = "ExpandedNavLink($select=StreamProp1,Namespace.AlwaysBindableAction1;$expand=ExpandedNavLink($select=StreamProp2,Namespace.AlwaysBindableAction1))";
            Assert.Equal(expectedPayload,
                this.GetWriterOutputForContentTypeAndKnobValue("application/json;odata.metadata=minimal", true, itemsToWrite, edmModel: Model, edmEntitySet: null, edmEntityType: EntityType, selectClause: selectClause, expandClause: expandClause));
        }

        #region compute id for containment in reader
        [Fact]
        public void ReadContainedEntityWithoutContextUrl()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
                    "\"ExpandedNavLink\":[{\"ID\":123,\"Name\":\"Bob\"}]," +
                    "\"AnotherContainedNavProp\":[{\"ID\":123,\"Name\":\"Bob\"}]," +
                    "\"AnotherContainedNonCollectionNavProp\":{\"ID\":123,\"Name\":\"Bob\"}," +
                    "\"ID\":1" +
                "}";

            var entryList = ReadPayload(payload, EntitySet, EntityType);

            ODataResource entry = entryList[0];
            Assert.Equal(new Uri("http://example.com/EntitySet(123)"), entry.Id);

            entry = entryList[1];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/AnotherContainedNavProp(123)"), entry.Id);

            entry = entryList[2];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/AnotherContainedNonCollectionNavProp"), entry.Id);

            entry = entryList[3];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)"), entry.Id);
        }

        [Fact]
        public void ReadContainedEntityOnDerivedWithoutContextUrl()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
                    "\"@odata.type\":\"#Namespace.DerivedType\", " +
                    "\"ExpandedNavLink\":[{\"ID\":123,\"Name\":\"Bob\"}]," +
                    "\"ContainedNavPropOnDerived\":[{\"ID\":123,\"Name\":\"Bob\"}]," +
                    "\"ContainedNonCollectionNavPropOnDerived\":{\"ID\":123,\"Name\":\"Bob\"}," +
                    "\"ID\":1" +
                "}";

            var entryList = ReadPayload(payload, EntitySet, EntityType);

            ODataResource entry = entryList[0];
            Assert.Equal(new Uri("http://example.com/EntitySet(123)"), entry.Id);

            entry = entryList[1];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/Namespace.DerivedType/ContainedNavPropOnDerived(123)"), entry.Id);

            entry = entryList[2];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/Namespace.DerivedType/ContainedNonCollectionNavPropOnDerived"), entry.Id);

            entry = entryList[3];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)"), entry.Id);
        }

        [Fact]
        public void ShouldThrowToAccessContainedIdIfParentIdIsNotPresent()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
                    "\"AnotherContainedNavProp\":[{\"ID\":123,\"Name\":\"Bob\"}]," +
                    "\"AnotherContainedNonCollectionNavProp\":{\"ID\":123,\"Name\":\"Bob\"}" +
                "}";

            var entryList = ReadPayload(payload, EntitySet, EntityType);

            ODataResource entry = entryList[0];
            Action getId = () => Assert.Equal(entry.Id, new Uri(""));
            getId.Throws<ODataException>(Strings.ODataMetadataBuilder_MissingParentIdOrContextUrl);

            entry = entryList[1];
            getId = () => Assert.Equal(entry.Id, new Uri(""));
            getId.Throws<ODataException>(Strings.ODataMetadataBuilder_MissingParentIdOrContextUrl);
        }


        [Fact]
        public void ShouldNotThrowToAccessContainedIdIfContextUrlIsPresent()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
                    "\"AnotherContainedNavProp@odata.context\":\"http://example.com/$metadata#EntitySet(1)/AnotherContainedNavProp\"," +
                    "\"AnotherContainedNavProp\":[{\"ID\":123,\"Name\":\"Bob\"}]," +
                    "\"AnotherContainedNonCollectionNavProp@odata.context\":\"http://example.com/$metadata#EntitySet(1)/AnotherContainedNonCollectionNavProp/$entity\"," +
                    "\"AnotherContainedNonCollectionNavProp\":{\"ID\":123,\"Name\":\"Bob\"}" +
                "}";

            var entryList = ReadPayload(payload, EntitySet, EntityType);

            ODataResource entry = entryList[0];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/AnotherContainedNavProp(123)"), entry.Id);

            entry = entryList[1];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/AnotherContainedNonCollectionNavProp"), entry.Id);
        }

        [Fact]
        public void ReadNestedContainedWithoutContextUrl()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
                    "\"ContainedNavProp\":[{" +
                        "\"ID\":11," +
                        "\"Name\":\"Bob\"," +
                        "\"AnotherContainedNavProp\":[{\"ID\":111,\"Name\":\"Bob\"}]," +
                        "\"AnotherContainedNonCollectionNavProp\":{\"ID\":112,\"Name\":\"Bob\"}" +
                    "}]," +
                    "\"ContainedNonCollectionNavProp\":{" +
                        "\"ID\":22," +
                        "\"Name\":\"Bob\"," +
                        "\"AnotherContainedNavProp\":[{\"ID\":221,\"Name\":\"Bob\"}]," +
                        "\"AnotherContainedNonCollectionNavProp\":{\"ID\":222,\"Name\":\"Bob\"}" +
                    "}," +
                    "\"ID\":1" +
                "}";

            var entryList = ReadPayload(payload, EntitySet, EntityType);

            ODataResource entry = entryList[0];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/ContainedNavProp(11)/AnotherContainedNavProp(111)"), entry.Id);
            Assert.Equal("Namespace.AnotherEntityType", entry.TypeName);

            entry = entryList[1];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/ContainedNavProp(11)/AnotherContainedNonCollectionNavProp"), entry.Id);

            entry = entryList[2];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/ContainedNavProp(11)"), entry.Id);

            entry = entryList[3];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/ContainedNonCollectionNavProp/AnotherContainedNavProp(221)"), entry.Id);

            entry = entryList[4];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/ContainedNonCollectionNavProp/AnotherContainedNonCollectionNavProp"), entry.Id);

            entry = entryList[5];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/ContainedNonCollectionNavProp"), entry.Id);

            entry = entryList[6];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)"), entry.Id);
        }

        [Fact]
        public void ReadNestedDerivedContainedWithoutContextUrl()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#AnotherEntitySet/$entity\"," +
                    "\"ContainedNavPropIsDerived\":[{" +
                        "\"ID\":11," +
                        "\"Name\":\"Bob\"," +
                        "\"AnotherContainedNavProp\":[{\"ID\":111,\"Name\":\"Bob\"}]," +
                        "\"AnotherContainedNonCollectionNavProp\":{\"ID\":112,\"Name\":\"Bob\"}" +
                    "}]," +
                    "\"ContainedNonCollectionNavPropIsDerived\":{" +
                        "\"ID\":22," +
                        "\"Name\":\"Bob\"," +
                        "\"AnotherContainedNavProp\":[{\"ID\":221,\"Name\":\"Bob\"}]," +
                        "\"AnotherContainedNonCollectionNavProp\":{\"ID\":222,\"Name\":\"Bob\"}" +
                    "}," +
                    "\"ID\":1" +
                "}";

            var entryList = ReadPayload(payload, AnotherEntitySet, AnotherEntityType);

            ODataResource entry = entryList[0];
            Assert.Equal(new Uri("http://example.com/AnotherEntitySet(1)/ContainedNavPropIsDerived(11)/AnotherContainedNavProp(111)"), entry.Id);
            Assert.Equal("Namespace.AnotherEntityType", entry.TypeName);

            entry = entryList[1];
            Assert.Equal(new Uri("http://example.com/AnotherEntitySet(1)/ContainedNavPropIsDerived(11)/AnotherContainedNonCollectionNavProp"), entry.Id);

            entry = entryList[2];
            Assert.Equal(new Uri("http://example.com/AnotherEntitySet(1)/ContainedNavPropIsDerived(11)"), entry.Id);

            entry = entryList[3];
            Assert.Equal(new Uri("http://example.com/AnotherEntitySet(1)/ContainedNonCollectionNavPropIsDerived/AnotherContainedNavProp(221)"), entry.Id);

            entry = entryList[4];
            Assert.Equal(new Uri("http://example.com/AnotherEntitySet(1)/ContainedNonCollectionNavPropIsDerived/AnotherContainedNonCollectionNavProp"), entry.Id);

            entry = entryList[5];
            Assert.Equal(new Uri("http://example.com/AnotherEntitySet(1)/ContainedNonCollectionNavPropIsDerived"), entry.Id);

            entry = entryList[6];
            Assert.Equal(new Uri("http://example.com/AnotherEntitySet(1)"), entry.Id);
        }

        [Fact]
        public void ReadDeepContainedWithoutContextUrl()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
                    "\"ExpandedNavLink\":[{" +
                        "\"@odata.type\":\"#Namespace.DerivedType\", " +
                        "\"ID\":123," +
                        "\"Name\":\"Bob\"," +
                        "\"ContainedNavPropOnDerived\":[{\"ID\":123,\"Name\":\"Bob\"}]," +
                        "\"AnotherContainedNonCollectionNavProp\":{\"ID\":123,\"Name\":\"Bob\"}" +
                    "}]," +
                    "\"ID\":1" +
                "}";

            var entryList = ReadPayload(payload, EntitySet, EntityType);

            ODataResource entry = entryList[0];
            Assert.Equal(new Uri("http://example.com/EntitySet(123)/Namespace.DerivedType/ContainedNavPropOnDerived(123)"), entry.Id);
            Assert.Equal("Namespace.AnotherEntityType", entry.TypeName);

            entry = entryList[1];
            Assert.Equal(new Uri("http://example.com/EntitySet(123)/AnotherContainedNonCollectionNavProp"), entry.Id);

            entry = entryList[2];
            Assert.Equal(new Uri("http://example.com/EntitySet(123)"), entry.Id);
        }

        [Fact]
        public void ShouldIgnoreContainedContextUrlInPayloadIfIsComputable()
        {
            const string payload =
                "{" +
                    "\"@odata.context\":\"http://example.com/$metadata#EntitySet/$entity\"," +
                    "\"ID\":1," +
                    "\"AnotherContainedNavProp@odata.context\":\"http://example.com/$metadata#EntitySet(123)/AnotherContainedNavProp\"," +
                    "\"AnotherContainedNavProp\":[{\"ID\":123,\"Name\":\"Bob\"}]," +
                    "\"AnotherContainedNonCollectionNavProp@odata.context\":\"http://example.com/$metadata#EntitySet(123)/AnotherContainedNonCollectionNavProp/$entity\"," +
                    "\"AnotherContainedNonCollectionNavProp\":{\"ID\":123,\"Name\":\"Bob\"}" +
                "}";

            var entryList = ReadPayload(payload, EntitySet, EntityType);

            ODataResource entry = entryList[0];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/AnotherContainedNavProp(123)"), entry.Id);

            entry = entryList[1];
            Assert.Equal(new Uri("http://example.com/EntitySet(1)/AnotherContainedNonCollectionNavProp"), entry.Id);
        }

        private List<ODataResource> ReadPayload(string payload, IEdmNavigationSource entitySet, IEdmStructuredType entityType)
        {
            InMemoryMessage message = new InMemoryMessage();
            message.SetHeader("Content-Type", "application/json;odata.metadata=minimal");
            message.Stream = new MemoryStream(Encoding.UTF8.GetBytes(payload));

            List<ODataResource> entryList = new List<ODataResource>();

            using (var messageReader = new ODataMessageReader((IODataResponseMessage)message, null, Model))
            {
                var reader = messageReader.CreateODataResourceReader(entitySet, entityType);
                while (reader.Read())
                {
                    switch (reader.State)
                    {
                        case ODataReaderState.ResourceEnd:
                            entryList.Add((ODataResource)reader.Item);
                            break;
                    }
                }
            }

            return entryList;
        }

        #endregion

        private string GetWriterOutputForEntryWithPayloadMetadata(
            string contentType,
            bool autoComputePayloadMetadata,
            string selectClause = null,
            bool enableWritingODataAnnotationWithoutPrefix = false)
        {
            ODataItem[] itemsToWrite = new ODataItem[] { this.entryWithPayloadMetadata, this.navLinkWithPayloadMetadata, this.expandedNavLinkWithPayloadMetadata, new ODataResourceSet() };
            return this.GetWriterOutputForContentTypeAndKnobValue(contentType, autoComputePayloadMetadata, itemsToWrite, Model, EntitySet, EntityType, selectClause, enableWritingODataAnnotationWithoutPrefix: enableWritingODataAnnotationWithoutPrefix);
        }

        private string GetWriterOutputForEntryWithOnlyData(
            string contentType,
            bool autoComputePayloadMetadata,
            string selectClause = null)
        {
            ODataItem[] itemsToWrite = new ODataItem[] { this.entryWithOnlyData, this.navLinkWithoutPayloadMetadata, this.expandedNavLinkWithoutPayloadMetadata, new ODataResourceSet() };
            return this.GetWriterOutputForContentTypeAndKnobValue(contentType, autoComputePayloadMetadata, itemsToWrite, Model, EntitySet, EntityType, selectClause);
        }

        private string GetWriterOutputForContentTypeAndKnobValue(string contentType, bool autoComputePayloadMetadata, ODataItem[] itemsToWrite, EdmModel edmModel, IEdmEntitySetBase edmEntitySet, EdmEntityType edmEntityType, string selectClause = null, string expandClause = null, string resourcePath = null, bool enableWritingODataAnnotationWithoutPrefix = false)
        {
            MemoryStream outputStream = new MemoryStream();
            var container = ContainerBuilderHelper.BuildContainer(null);
            container.GetRequiredService<ODataSimplifiedOptions>().SetOmitODataPrefix(enableWritingODataAnnotationWithoutPrefix);
            IODataResponseMessage message = new InMemoryMessage() { Stream = outputStream, Container = container };

            message.SetHeader("Content-Type", contentType);
            ODataMessageWriterSettings settings = new ODataMessageWriterSettings();
            var result = new ODataQueryOptionParser(edmModel, edmEntityType, edmEntitySet, new Dictionary<string, string> { { "$select", selectClause }, { "$expand", expandClause } }).ParseSelectAndExpand();

            ODataUri odataUri = new ODataUri()
            {
                ServiceRoot = new Uri("http://example.com"),
                SelectAndExpand = result
            };

            if (resourcePath != null)
            {
                Uri requestUri = new Uri("http://example.com/" + resourcePath);
                odataUri.RequestUri = requestUri;
                odataUri.Path = new ODataUriParser(edmModel, new Uri("http://example.com"), requestUri).ParsePath();
            }

            settings.ODataUri = odataUri;

            string output;
            using (var messageWriter = new ODataMessageWriter(message, settings, edmModel))
            {
                int currentIdx = 0;

                if (itemsToWrite[currentIdx] is ODataResourceSet)
                {
                    ODataWriter writer = messageWriter.CreateODataResourceSetWriter(edmEntitySet, edmEntityType);
                    this.WriteFeed(writer, itemsToWrite, ref currentIdx);
                }
                else if (itemsToWrite[currentIdx] is ODataResource)
                {
                    ODataWriter writer = messageWriter.CreateODataResourceWriter(edmEntitySet, edmEntityType);
                    this.WriteEntry(writer, itemsToWrite, ref currentIdx);
                }
                else
                {
                    Assert.True(false, "Top level item to write must be entry or feed.");
                }

                Assert.Equal(itemsToWrite.Length, currentIdx);

                outputStream.Seek(0, SeekOrigin.Begin);
                output = new StreamReader(outputStream).ReadToEnd();
            }

            return output;
        }

        private void WriteFeed(ODataWriter writer, ODataItem[] itemsToWrite, ref int currentIdx)
        {
            if (currentIdx < itemsToWrite.Length)
            {
                ODataResourceSet feed = (ODataResourceSet)itemsToWrite[currentIdx++];
                writer.WriteStart(feed);
                while (currentIdx < itemsToWrite.Length && itemsToWrite[currentIdx] is ODataResource)
                {
                    this.WriteEntry(writer, itemsToWrite, ref currentIdx);
                }

                writer.WriteEnd();
            }
        }

        private void WriteEntry(ODataWriter writer, ODataItem[] itemsToWrite, ref int currentIdx)
        {
            if (currentIdx < itemsToWrite.Length)
            {
                ODataResource entry = (ODataResource)itemsToWrite[currentIdx++];
                writer.WriteStart(entry);
                while (currentIdx < itemsToWrite.Length && itemsToWrite[currentIdx] is ODataNestedResourceInfo)
                {
                    this.WriteLink(writer, itemsToWrite, ref currentIdx);
                }

                writer.WriteEnd();
            }
        }

        private void WriteLink(ODataWriter writer, ODataItem[] itemsToWrite, ref int currentIdx)
        {
            if (currentIdx < itemsToWrite.Length)
            {
                ODataNestedResourceInfo link = (ODataNestedResourceInfo)itemsToWrite[currentIdx++];
                writer.WriteStart(link);
                if (currentIdx < itemsToWrite.Length)
                {
                    if (itemsToWrite[currentIdx] is ODataResource)
                    {
                        this.WriteEntry(writer, itemsToWrite, ref currentIdx);
                    }
                    else if (itemsToWrite[currentIdx] is ODataResourceSet)
                    {
                        this.WriteFeed(writer, itemsToWrite, ref currentIdx);
                    }
                }

                writer.WriteEnd();
            }
        }
    }
}
