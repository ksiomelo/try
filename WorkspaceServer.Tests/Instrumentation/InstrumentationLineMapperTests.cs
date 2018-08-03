﻿using FluentAssertions;
using System.Linq;
using WorkspaceServer.Models.Execution;
using WorkspaceServer.Servers.Roslyn.Instrumentation;
using WorkspaceServer.Tests.Servers.Roslyn.Instrumentation;
using WorkspaceServer.Transformations;
using Xunit;

namespace WorkspaceServer.Tests.Instrumentation
{
    public class InstrumentationLineMapperTests
    {
        private (AugmentationMap, VariableLocationMap, Microsoft.CodeAnalysis.Document, Viewport) Setup(string code)
        {
            var withLF = CodeManipulation.EnforceLF(code);
            var document = Sources.GetDocument(withLF);
            var workspace = new Workspace(files: new[] { new Workspace.File("test.cs", withLF) });
            var visitor = new InstrumentationSyntaxVisitor(document);
            var viewport = new BufferInliningTransformer().ExtractViewPorts(workspace).DefaultIfEmpty(null).First();
            return (visitor.Augmentations, visitor.VariableLocations, document, viewport);
        }

        [Fact]
        public async System.Threading.Tasks.Task MapLineLocationsRelativeToViewport_Does_Nothing_Without_ViewportAsync()
        {
            var (augmentation, locations, document, _) = Setup(Sources.simple);
            var (newAugmentation, newLocations) = await InstrumentationLineMapper.MapLineLocationsRelativeToViewportAsync(augmentation, locations, document);

            augmentation.Should().BeEquivalentTo(newAugmentation);
            locations.Should().BeEquivalentTo(newLocations);
        }

        [Fact]
        public async System.Threading.Tasks.Task MapLineLocationsRelativeToViewport_Maps_Augmentation_FilePosition_CorrectlyAsync()
        {
            var (augmentation, locations, document, viewport) = Setup(Sources.withLocalParamsAndRegion);
            var (newAugmentation, newLocations) = await InstrumentationLineMapper.MapLineLocationsRelativeToViewportAsync(augmentation, locations, document, viewport);

            var linePositions = newAugmentation.Data.Values.Select(state => state.CurrentFilePosition.Line);
            linePositions.Should().Equal(new[] { 0, 1 });
        }

        [Fact]
        public async System.Threading.Tasks.Task MapLineLocationsRelativeToViewport_Maps_Variable_Location_CorrectlyAsync()
        {

            var (augmentation, locations, document, viewport) = Setup(Sources.withLocalParamsAndRegion);

            var (newAugmentation, newLocations) = await InstrumentationLineMapper.MapLineLocationsRelativeToViewportAsync(augmentation, locations, document, viewport);
            var variableLocationLines = newLocations.Data.Values
                .SelectMany(locs => locs)
                .Select(loc => loc.StartLine);

            variableLocationLines.Should().Equal(new[] { 0 });
        }

        [Fact]
        public void FilterActiveViewport_Should_Return_Viewport_In_ActiveBufferId()
        {
            var text = CodeManipulation.EnforceLF(Sources.withMultipleRegion);
            var workspace = new Workspace(files: new[] { new Workspace.File("testFile.cs", text) });
            var viewports = new BufferInliningTransformer().ExtractViewPorts(workspace);
            var activeViewport = InstrumentationLineMapper.FilterActiveViewport(viewports, "testFile.cs@test").First();
            activeViewport.Region.Start.Should().Be(128);
        }

        [Fact]
        public void FilterActiveViewport_Should_Return_Empty_Array_If_No_Regions()
        {
            var text = CodeManipulation.EnforceLF(Sources.simple);
            var workspace = new Workspace(files: new[] { new Workspace.File("testFile.cs", text) });
            var viewports = new BufferInliningTransformer().ExtractViewPorts(workspace);
            var activeViewport = InstrumentationLineMapper.FilterActiveViewport(viewports, "testFile.cs@test");
            activeViewport.Should().BeEmpty();
        }
    }
}

