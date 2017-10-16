using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;

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
            // for the view model.
            ControlID = ID_embeddable_control;
            
        }

        /// <summary>
        /// prepares the content for the embeddable control when this tool is activated (i.e. when this tool is clicked). This is the ENTRY POINT!
        /// This class is changed to return async
        /// </summary>
        protected override async Task OnToolActivateAsync(bool active) {

            if (_attributeViewModel == null)
                _attributeViewModel = this.EmbeddableControl as AttributeControlViewModel;
            // this.EmbeddableControl is the one with ID set as ControlID (see constructor)

            //return base.OnToolActivateAsync(active);
            // INITIATE the task to execute on the MCT (Main CIM Thread)
            // all Geospatial API MUST BE passed on to QueuedTask.Run, or it will
            // throw exception
            await QueuedTask.Run( () => {
                // get the selection in dictionary form
                var selectionInMap = ActiveMapView.Map.GetSelection();
                var pointLayer = ActiveMapView.Map.GetLayersAsFlattenedList().OfType<FeatureLayer>().Where(layer => layer.ShapeType == ArcGIS.Core.CIM.esriGeometryType.esriGeometryPoint).FirstOrDefault();

                
                if (pointLayer != null) {
                    
                    if (selectionInMap.ContainsKey(pointLayer)) {
                        // if the point layer contains selected features
                        var selectionDictionary = new Dictionary<MapMember, List<long>>();
                        selectionDictionary.Add(pointLayer as MapMember, selectionInMap[pointLayer]);

                        // store this in the view model to populate the tree view
                        _attributeViewModel.SelectedFeatures = selectionDictionary;

                        // load the FIRST selected point feature (thus using [0])
                        _attributeViewModel.AttributeInspector.Load(pointLayer, selectionInMap[pointLayer][0]);
                    }
                }

                return Task.FromResult(true);

            } );
        }

        protected override Task<bool> OnSketchCompleteAsync(Geometry geometry) {
            return base.OnSketchCompleteAsync(geometry);

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
