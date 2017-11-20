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

        private AttributeControlViewModel _attributeViewModel = null;
        private MapPoint CurrentControlPoint;

        public ShiftScaleTool() {
            // indicate that you need feedback graphics
            IsSketchTool = true;

            // type is initially set to rectangle => user chooses top-left & bottom right points.
            // use this attribute to also distinguish what the user has drawn, point or rectangle
            SketchType = SketchGeometryType.Rectangle;

            // the coordinate of the sketched rectangle is returned in map coordinates.
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
            RestoreBeforeExit();
            // deselect the currently selected ones?
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

        public async void ShiftAndScaleFeatures() {
            if (CurrentControlPoint == null) {
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Control point not selected", "Error");
                return;
            }

            double dx, dy, dz, scale;
            getOffsetVectorAndScale(out dx, out dy, out dz, out scale);

            ///////
            var featuresToShift = _attributeViewModel.SelectedFeatures;
            EditOperation shiftOperation = new EditOperation {
                Name = "Shift Features",
                ErrorMessage = "Error during shift",
                ProgressMessage = "Shifting in progress",
                SelectModifiedFeatures = true,
                ShowModalMessageAfterFailure = false
            };
            shiftOperation.Move(featuresToShift, dx, dy, dz);
            //shiftOperation.Scale(featuresToShift, origin, scale, scale);
            bool success = await shiftOperation.ExecuteAsync();

            //////////
            int countFeatures = 0;
            foreach (List<long> value in _attributeViewModel.SelectedFeatures.Values) {
                countFeatures += value.Count;
            }
            string message = success ? "success\n" : "failed\n";
             
            ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(string.Format(message + "dx = {0:0.000}\ndy = {1:0.000}\ndz  = {2:0.000} \nNumber of features affected = {3}", dx, dy, dz, countFeatures), "Shifting and Scaling");

            RestoreUIAfterShiftAndScale();
            SwitchToRectangleSketch();
        }

        /// <summary>
        /// returns the offset vector (ControlPoint - (x, y, z)) and the scale from inputs
        /// </summary>
        private void getOffsetVectorAndScale(out double dx, out double dy, out double dz, out double scale) {
            // correct this as this is comparing metres to coordinates, depending on the
            // coordinate system.
            dx = _attributeViewModel.X - CurrentControlPoint.X;
            dy = _attributeViewModel.Y - CurrentControlPoint.Y;
            dz = 0;
            scale = _attributeViewModel.Scale;
        }

        public Task<bool> ApplySelectionWithRectangle(Geometry rectangle) {
            // select the first point feature layer in the active map
            var pointLayer = ActiveMapView.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(layer => layer.ShapeType == ArcGIS.Core.CIM.esriGeometryType.esriGeometryPoint).FirstOrDefault();

            if (pointLayer == null)
                return Task.FromResult(false);

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

        public void RestoreUIAfterShiftAndScale() {
            _attributeViewModel.UserPromptText = "Pick a selection";
            _attributeViewModel.HasUserSelectedFeatures = false;
        }

        public void SwitchToPointSketch() {
            SketchType = SketchGeometryType.Point;
        }

        public void SwitchToRectangleSketch() {
            SketchType = SketchGeometryType.Rectangle;
            if (CurrentControlPoint != null)
                CurrentControlPoint = null;
        }

        public void RestoreBeforeExit() {
            // _attributeViewModel is set to null after this, so no need to store its settings
            SketchType = SketchGeometryType.Rectangle;
            if (_attributeViewModel != null) {
                _attributeViewModel.PickControlButtonClicked -= SwitchToPointSketch;
                _attributeViewModel.ShiftAndScaleButtonClicked -= ShiftAndScaleFeatures;
                _attributeViewModel = null;
            }
        }

        /// <summary>
        /// Ascertains that when a left mouse button is clicked, whether this click is to select the control point or not
        /// </summary>
        /// <param name="e"></param>
        protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e) {
            if (e.ChangedButton == System.Windows.Input.MouseButton.Left && SketchType == SketchGeometryType.Point)
                //Handle the event args to get the call to the corresponding async method
                e.Handled = true;
        }
        /// <summary>
        /// Sets the control point to the clicked location
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e) {
            return QueuedTask.Run(() => {
                // this line must be called from within Run()
                CurrentControlPoint = MapView.Active.ClientToMap(e.ClientPoint);
                // notify the user the control point is selected
                ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show(string.Format("X: {0:0.000} \nY: {1:0.000} \nZ: {2:0.000}", CurrentControlPoint.X, CurrentControlPoint.Y, CurrentControlPoint.Z), "Control Point Picked");
            });
            
        }
    }

}
