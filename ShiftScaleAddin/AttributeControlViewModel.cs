using System.Collections.Generic;
using ArcGIS.Desktop.Framework.Controls;
using System.Xml.Linq;
using ArcGIS.Desktop.Mapping;
using System.Windows.Controls;
using ArcGIS.Desktop.Editing.Attributes;
using ActiproSoftware.Windows.Input;
using System;
using System.Windows.Input;

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

        /// <summary>
        /// event used to notify ShiftScaleTool class that user is changing the SketchType
        /// </summary>
        public Action PickControlButtonClicked;

        private DelegateCommand<string> _pickControlPointCommand;
        public DelegateCommand<string> PickControlCommand {
            get { return _pickControlPointCommand; }
        }

        /// <summary>
        /// event used to onotify ShiftScaleTool class that user wants to shift and scale selected elements
        /// </summary>
        public Action ShiftAndScaleButtonClicked;

        private DelegateCommand<string> _shiftAndScaleCommand;
        public DelegateCommand<string> ShiftControlCommand {
            get { return _shiftAndScaleCommand; }
        }

        // Values binded to by the form
        private int _x;
        public int X {
            get { return _x; }
            set {
                SetProperty(ref _x, value, () => X);
            }
        }

        private int _y;
        public int Y {
            get { return _y; }
            set {
                SetProperty(ref _y, value, () => Y);
            }
        }

        private int _z;
        public int Z {
            get { return _z; }
            set {
                SetProperty(ref _z, value, () => Z);
            }
        }

        private int _scale;
        public int Scale {
            get { return _scale; }
            set {
                SetProperty(ref _scale, value, () => Scale);
            }
        }

        public AttributeControlViewModel(XElement options, bool canChangeOptions) : base(options, canChangeOptions) {

            _pickControlPointCommand = new DelegateCommand<string>(
                (s) => { PickControlButtonClicked.Invoke();  } // this lambda is for Execute. This tells the MapTool to change sketchtype
                );

            _shiftAndScaleCommand = new DelegateCommand<string>(
                (s) => { ShiftAndScaleButtonClicked.Invoke();  }
                    // check that the values in the form are all valid numbers
                    // this can be done using MaskedTestBox
                );
        }

    }

}
