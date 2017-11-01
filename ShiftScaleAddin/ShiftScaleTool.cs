using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Desktop.Editing;

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
        private MapPoint CurrentControlPoint;

        public ShiftScaleTool() {

            // indicate that you need feedback graphics
            IsSketchTool = true;

            // type is initially set to rectangle => user chooses top-left & bottom right points
            // use this attribute to also distinguish what the user has drawn, point or rectangle
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
        /// </summary>
        protected override Task OnToolActivateAsync(bool active) {
            if (_attributeViewModel == null) {
                _attributeViewModel = this.EmbeddableControl as AttributeControlViewModel;
                _attributeViewModel.PickControlButtonClicked += SwitchToPointSketch;
                _attributeViewModel.ShiftAndScaleButtonClicked += ShiftAndScaleFeatures;
            }

            _attributeViewModel.UserPromptText = "Make a selection";
            _attributeViewModel.HasUserSelectedFeatures = false;

            return base.OnToolActivateAsync(active);
        }

        protected override Task OnToolDeactivateAsync(bool hasMapViewChanged) {
            _attributeViewModel = null;
            RestoreBeforeExit();
            // deselect the currently selected ones
            return Task.FromResult(true);
        }

        /// <summary>
        /// This method is event handler which is called when the user completes
        /// drawing on the map.
        /// </summary>
        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry) {
            if (SketchType == SketchGeometryType.Rectangle) {
                return ApplySelectionWithRectangle(geometry);
            }
            else if (SketchType == SketchGeometryType.Point) {
                // do nothing, as it will be handled on onToolMouseDown
            }

            return Task.FromResult(true);
        }

        public void ShiftAndScaleFeatures() {
            /* Reasons for using EditOperation class
                1. Execute the operations against underlying datastores
                2. Add an operation to the ArcGIS Pro OperationManager Undo / Redo stack
                3. Invalidate any layer caches associated with the layers edited by the Edit Operation.

                EditOperation is transaction-based: similar to SQL, if any part of the transaction fails, the transaction is aborted and rolled-back.
            */
            QueuedTask.Run(() => {
                EditOperation scaleOperation = new EditOperation();
                scaleOperation.Name = "Shift and Scale features";
                double scale = _attributeViewModel.Scale;
                scaleOperation.Scale(_attributeViewModel.SelectedFeatures, CurrentControlPoint, scale, scale, scale);
                scaleOperation.Execute();

                var shiftOperation = scaleOperation.CreateChainedOperation();
                double dx = _attributeViewModel.X - CurrentControlPoint.X;
                double dy = _attributeViewModel.Y - CurrentControlPoint.Y;
                double dz = _attributeViewModel.Z - CurrentControlPoint.Z;

                shiftOperation.Move(_attributeViewModel.SelectedFeatures, dx, dy, dz);
                shiftOperation.Execute();

            }); // end Run

            // do we need to return queuedTask.FromResult(true)?
        }

        public Task<bool> ApplySelectionWithRectangle(Geometry rectangle) {
            // select the first point feature layer in the active map
            var pointLayer = ActiveMapView.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(layer => layer.ShapeType == ArcGIS.Core.CIM.esriGeometryType.esriGeometryPoint).FirstOrDefault();

            if (pointLayer == null)
                return Task.FromResult(true);

            // execute on the MCT (Main CIM Thread). All Geospatial API MUST BE passed on to QueuedTask.Run as follows, or it will throw exception
            QueuedTask.Run(() => {
                var spatialQuery = new SpatialQueryFilter() {
                    // use the selected geometry to filter out elements outside the geometry
                    FilterGeometry = rectangle,
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

            UpdateUIAfterSelection();

            return Task.FromResult(true);
        }

        public void UpdateUIAfterSelection() {
            // now that the user has made selection, change the message
            _attributeViewModel.UserPromptText = "Change the selection";
            // and display additional UIs such as forms, buttons etc.
            _attributeViewModel.HasUserSelectedFeatures = true;
        }

        public void SwitchToPointSketch() {
            SketchType = SketchGeometryType.Point;
        }

        public void RestoreBeforeExit() {
            // _attributeViewModel is set to null after this, so no need to store its settings
            SketchType = SketchGeometryType.Rectangle;
            if (_attributeViewModel != null) {
                _attributeViewModel.PickControlButtonClicked -= SwitchToPointSketch;
                _attributeViewModel.ShiftAndScaleButtonClicked -= ShiftAndScaleFeatures;
            }
        }

        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e) {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left && SketchType == SketchGeometryType.Point)
                //Handle the event args to get the call to the corresponding async method
                e.Handled = true;
        }

        protected override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e) {
            return QueuedTask.Run(() => {
                CurrentControlPoint = MapView.Active.ClientToMap(e.ClientPoint);

                // for testing
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(string.Format("X: {0} Y: {1} Z: {2}", CurrentControlPoint.X, CurrentControlPoint.Y, CurrentControlPoint.Z), "Map Coordinates");
            });

            
        }
    }

}
