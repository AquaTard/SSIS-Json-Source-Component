﻿using System;
using System.Windows.Forms;
using Microsoft.SqlServer.Dts.Runtime;
using Microsoft.SqlServer.Dts.Pipeline.Design;
using Microsoft.SqlServer.Dts.Pipeline.Wrapper;
using JSONSource;
using Microsoft.SqlServer.Dts.Design;
using Microsoft.SqlServer.Dts.Runtime.Design;
using System.Collections.Generic;

namespace com.webkingsoft.JSONSource_120
{

    /*
     * Costituisce il controller della view.
     */
    public class JSONTransformComponentUI : IDtsComponentUI
    {
        private IDTSComponentMetaData100 _md;
        private IServiceProvider _sp;
        private TransformationModel _model;
        public void Help(System.Windows.Forms.IWin32Window parentWindow)
        {
        }

        public void New(System.Windows.Forms.IWin32Window parentWindow)
        {
            
        }
        public void Delete(System.Windows.Forms.IWin32Window parentWindow)
        {
        }

        public bool Edit(System.Windows.Forms.IWin32Window parentWindow, Variables vars, Connections cons)
        {
            // Create and display the form for the user interface.
            _model.Inputs.Clear();
            IDTSInput100 input = _md.InputCollection[0];
            foreach (IDTSVirtualInputColumn100 vcol in input.GetVirtualInput().VirtualInputColumnCollection) {
                // Only add the textual columns as valid input.
                if (vcol.DataType == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_NTEXT || vcol.DataType == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_TEXT || vcol.DataType == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_STR || vcol.DataType == Microsoft.SqlServer.Dts.Runtime.Wrapper.DataType.DT_WSTR)
                    _model.Inputs.Add(vcol.Name, vcol.LineageID);
            }

            JsonTransformUI componentEditor = new JsonTransformUI(parentWindow, vars, _model, _sp);


            DialogResult result = componentEditor.ShowDialog(parentWindow);

            if (result == DialogResult.OK)
            {
                _md.CustomPropertyCollection[JSONSourceComponent.PROPERTY_KEY_MODEL].Value = _model.ToJsonConfig();
                AddInputColumn(_model.InputColumnName);
                AddOutputColumns(_model.IoMap);
                return true;
            }
            return false;
        }

        private void AddInputColumn(string vcolInputName)
        {
            var input = _md.InputCollection[0];
            var virtualInputs = _md.InputCollection[0].GetVirtualInput();
            IDTSVirtualInputColumn100 vcol = null;
            foreach (IDTSVirtualInputColumn100 vc in virtualInputs.VirtualInputColumnCollection)
            {
                if (vcolInputName == vc.Name) {
                    vcol = vc;
                    break;
                }
            }
            if (vcol == null)
                // Non ho trovato la colonna!
                throw new Exception("Metadata are broken: input column "+vcolInputName+" has not been found among the inputs of this component. Please refresh its metadata by editing the component.");

            // Pulisci ogni input già assegnato ed assegna quello scelto
            input.InputColumnCollection.RemoveAll();
            var incol = input.InputColumnCollection.New();
            incol.LineageID = vcol.LineageID;
            /*
            CManagedComponentWrapper destDesignTime = _md.Instantiate();
            // Il metodo seguente effettua il mapping tra una VirtualInputColumn (che di fatto corrisponde all'output del componente in gerarchia) ed una colonna fisica
            // di questo componente. Nel nostro caso abbiamo una sola colonna, quindi ci basta eseguire questo metodo una sola volta.
            destDesignTime.SetUsageType(input.ID, virtualInputs, vcol.LineageID, DTSUsageType.UT_READONLY);
             * */
        }

        private void AddOutputColumns(IEnumerable<IOMapEntry> IoMap)
        {
            // Tutto andato a buonfine: aggiorna le colonne di output:
            _md.OutputCollection[0].OutputColumnCollection.RemoveAll();
            foreach (IOMapEntry e in IoMap)
            {
                if (e.InputFieldLen < 0)
                {
                    // FIXME TODO
                    _md.FireWarning(0, _md.Name, "A row of the IO configuration presents a negative value, which is forbidden.", null, 0);
                }

                // Creo la nuova colonna descritta dalla riga e la configuro in base ai dettagli specificati
                IDTSOutputColumn100 col = _md.OutputCollection[0].OutputColumnCollection.New();
                col.Name = e.OutputColName;
                col.SetDataTypeProperties(e.OutputColumnType, e.InputFieldLen, 0, 0, 0);
            }
        }


        /*
         * Metodo invocato quando il componente UI viene caricato per la prima volta, generalmente in seguito al doppio click sul componente.
         */
        public void Initialize(IDTSComponentMetaData100 dtsComponentMetadata, IServiceProvider serviceProvider)
        {
            // Salva un link ai metadati del runtime editor ed al serviceProvider
            _sp = serviceProvider;
            _md = dtsComponentMetadata;

            // Controlla se l'oggetto contiene il model serializzato nelle proprietà. In caso negativo, creane uno nuovo ed attribuisciglielo.
            IDTSCustomProperty100 model = _md.CustomPropertyCollection[JSONSourceComponent.PROPERTY_KEY_MODEL];
            if (model.Value == null)
            {
                _model = new TransformationModel();
                model.Value = _model.ToJsonConfig();
            }
            else
                _model = TransformationModel.LoadFromJson(model.Value.ToString());

            if (_md == null)
                _md = (IDTSComponentMetaData100)_md.Instantiate();

        }
    }
}