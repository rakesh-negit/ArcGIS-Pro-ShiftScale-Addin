using System.Collections.Generic;
using ArcGIS.Desktop.Framework.Controls;
using System.Xml.Linq;
using ArcGIS.Desktop.Mapping;
using System.Windows.Controls;
using ArcGIS.Desktop.Editing.Attributes;

namespace ShiftScaleAddin {
    internal class AttributeControlViewModel : EmbeddableControl {
        private EmbeddableControl _inspectorViewModel = null;
        public EmbeddableControl InspectorViewModel {
            get { return _inspectorViewModel; }
            set {
                if (value != null) {
                    _inspectorViewModel = value;
                    _inspectorViewModel.OpenAsync();

                }
                else if (_inspectorViewModel != null) {
                    _inspectorViewModel.CloseAsync();
                    _inspectorViewModel = value;
                }
                NotifyPropertyChanged(() => InspectorViewModel);
            }
        }

        private UserControl _inspectorView = null;
        public UserControl InspectorView {
            get { return _inspectorView; }
            set {
                SetProperty(ref _inspectorView, value, () => InspectorView);
            }
        }

        // this dictionary contains the selected features in the map to populate the tree view for layres and respective selected features.
        private Dictionary<MapMember, List<long>> _selectedFeatures = null;
        public Dictionary<MapMember, List<long>> SelectedFeatures {
            get { return _selectedFeatures; }
            set {
                // calling SetProperty is important as it calls NotifyPropertyChanged
                SetProperty(ref _selectedFeatures, value, () => SelectedFeatures);
            }

        }

        public Inspector AttributeInspector { get; }

        public AttributeControlViewModel(XElement options, bool canChangeOptions) : base(options, canChangeOptions) {

            // create a new instance for the inspector. This is used a lot in ShiftScaleTool
            AttributeInspector = new Inspector();
            // create an embeddable control from the inspector class to display on the pane
            var icontrol = AttributeInspector.CreateEmbeddableControl();

            // get view and viewmodel from the inspector
            InspectorView = icontrol.Item2;
            InspectorViewModel = icontrol.Item1;
        }

        /// <summary>
        /// Text shown in the control.
        /// </summary>
        private string _text = "Embeddable Control";
        public string Text {
            get { return _text; }
            set {
                SetProperty(ref _text, value, () => Text);
            }
        }
    }
}
