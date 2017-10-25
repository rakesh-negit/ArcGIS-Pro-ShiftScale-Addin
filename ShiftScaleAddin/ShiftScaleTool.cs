using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Data;

namespace ShiftScaleAddin {
    internal class ShiftScaleTool : MapTool {
        public const string ID_embeddable_control = "ShiftScaleAddin_AttributeControl";
        #region Note
        // the most important methods in this class are:
        // 1. OnSketchCompleted
        // 2. OnSketchModified
        // 3. OnToolActivateAsync
        // 4. OnTOolDeactivateAsync

        // Only geometries in screen coordinates are supported in 3D

        #endregion

        private AttributeControlViewModel _attributeViewModel = null;

        // differentiate between the user selecting rectangle vs the point
        private enum DrawingStage {
            SelectingFeatures,
            SelectingPoint
        }
        private DrawingStage currentStage;

        public ShiftScaleTool() {

            // indicate that you need feedback graphics
            IsSketchTool = true;

            // type is set to rectangle => user chooses top-left & bottom right points
            SketchType = SketchGeometryType.Rectangle;

            // the cooridnate of the sketched rectangle is returned in map coordinates.
            // the alternative is SketchOutputMode.Screen
            SketchOutputMode = SketchOutputMode.Map;

            // specify the (inherited) ID for the embeddable content AS DECLARED IN config.daml
            // AND specified in the AttributeCotrol.xaml and AttributeControlViewModel
            // for the view model. We use this ControlID to retrieve the EmbeddableControl
            ControlID = ID_embeddable_control;
        }

        /// <summary>
        /// prepares the content for the embeddable control when this tool is activated (i.e. when this tool is clicked). This is the ENTRY POINT!
        /// This class is changed to return async
        /// </summary>
        protected override Task OnToolActivateAsync(bool active) {
            // let the user sketch a rectangle first
            currentStage = DrawingStage.SelectingFeatures; 

            if (_attributeViewModel == null)
                _attributeViewModel = this.EmbeddableControl as AttributeControlViewModel;

            _attributeViewModel.UserPromptText = "Make a selection";
            _attributeViewModel.HasUserSelectedFeatures = false;

            return base.OnToolActivateAsync(active);
        }

        protected override Task OnToolDeactivateAsync(bool hasMapViewChanged) {
            _attributeViewModel = null;
            return Task.FromResult(true);
        }

        /// <summary>
        /// This method is event handler which is called when the user completes
        /// drawing on the map.
        /// </summary>
        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry) {
            // select the first point feature layer in the active map
            var pointLayer = ActiveMapView.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(layer => layer.ShapeType == ArcGIS.Core.CIM.esriGeometryType.esriGeometryPoint).FirstOrDefault();

            if (pointLayer == null)
                return Task.FromResult(true);

            // execute on the MCT (Main CIM Thread). All Geospatial API MUST BE passed on to QueuedTask.Run as follows, or it will throw exception
            QueuedTask.Run(() => {
                var spatialQuery = new SpatialQueryFilter() {
                    // use the selected geometry to filter out elements outside the geometry
                    FilterGeometry = geometry, 
                    SpatialRelationship = SpatialRelationship.Contains
                };
                
                // apply the spatial filter to the pointLayer
                var pointSelection = pointLayer.Select(spatialQuery);
                List<long> oids = pointSelection.GetObjectIDs().ToList();
                if (oids.Count == 0)
                    return;

                var selectionDictionary = new Dictionary<MapMember, List<long>>();
                selectionDictionary.Add(pointLayer as MapMember, pointSelection.GetObjectIDs().ToList());

                // assign the dictionary to the ViewModel such that the View will update
                _attributeViewModel.SelectedFeatures = selectionDictionary;

            }); // end Run

            // now that the user has made selection, change the message
            _attributeViewModel.UserPromptText = "Change the selection";

            // and display additional UI
            _attributeViewModel.HasUserSelectedFeatures = true;

            return Task.FromResult(true);

            // TODO: There has to be some EditOperation for us to actually move the objects
            // to the selected point

            /* Reasons for using EditOperation class
            1. Execute the operations against underlying datastores
            2. Add an operation to the ArcGIS Pro OperationManager Undo / Redo stack
            3. Invalidate any layer caches associated with the layers edited by the Edit Operation.

            EditOperation is transaction-based: similar to SQL, if any part of the transaction fails, the transaction is aborted and rolled-back.

            Most likely be using EditOperation.Transform() to move stuff
            */
        }

    }
}
