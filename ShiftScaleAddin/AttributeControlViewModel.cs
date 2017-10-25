using System.Collections.Generic;
using ArcGIS.Desktop.Framework.Controls;
using System.Xml.Linq;
using ArcGIS.Desktop.Mapping;
using System.Windows.Controls;
using ArcGIS.Desktop.Editing.Attributes;

namespace ShiftScaleAddin {
    internal class AttributeControlViewModel : EmbeddableControl {

        private Dictionary<MapMember, List<long>> _selectedFeatures = null;
        public Dictionary<MapMember, List<long>> SelectedFeatures {
            get { return _selectedFeatures; }
            set {
                // calling SetProperty is important as it calls NotifyPropertyChanged
                SetProperty(ref _selectedFeatures, value, () => SelectedFeatures);
            }

        }

        /// <summary>
        /// Message that prompts user to make/change selection
        /// </summary>
        private string _userPromptText = null;
        public string UserPromptText {
            get { return _userPromptText; }
            set {
                SetProperty(ref _userPromptText, value, () => UserPromptText);
            }
        }

        private bool _hasUserSelectedFeatures;
        public bool HasUserSelectedFeatures {
            get { return _hasUserSelectedFeatures; }
            set {
                SetProperty(ref _hasUserSelectedFeatures, value, () => HasUserSelectedFeatures);
            }
        }

        public AttributeControlViewModel(XElement options, bool canChangeOptions) : base(options, canChangeOptions) {
        }
    }
}
