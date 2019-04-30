using HoloToolkit.Unity.Buttons;
using System.Collections;
using System.Collections.Generic;
using ThesisHololens.Devices;
using ThesisHololens.Manipulation;
using ThesisHololens.Settings;
using ThesisHololens.UI;
using UnityEngine;


namespace ThesisHololens.UI.EditorMenu
{
    public class EditorModeManager : MonoBehaviour
    {

        public enum EditorState
        {
            inactive,
            active

        }

        private EditorState state = EditorState.inactive;
        public EditorState State
        {
            get
            {
                return state;
            }
            private set
            {
                state = value;
            }
        }

        public void startEditorMode()
        {
            DeviceManager.Instance.GetComponent<ToggleEdit>().toggleEditOn();
            UIContainerBar.Instance.GetComponent<ToggleEdit>().toggleEditOn();
            EditorManager.Instance.startmenuFresh();//new device


            foreach (var obj in FindObjectsOfType<WorldAnchorUserForFixedElements>())
            {

                obj.activatemanipulation();
            }

            State = EditorState.active;
            //TODO: self active
        }

        public void endEditorMode()
        {
            //triggers the saving of the UI data, that could have been manipulated
            UIContainerBar.Instance.Target = null;

            DeviceManager.Instance.GetComponent<ToggleEdit>().toggleEditOff();
            UIContainerBar.Instance.GetComponent<ToggleEdit>().toggleEditOff();
            EditorManager.Instance.EndEditor();//new device

            foreach (var obj in FindObjectsOfType<WorldAnchorUserForFixedElements>())
            {

                obj.deactivatemanipulation();
            }

            State = EditorState.inactive;

        }

        public void ModifyDevice()
        {
            if (State == EditorState.inactive)
                return;

            GameObject target = UIContainerBar.Instance.Target;

            if (target != null)
            {
                EditorManager.Instance.startmenuWithExistingItem(target.GetComponent<Device>().DeviceData);
            }
        }

        public void removeDevice()
        {

            if (State == EditorState.inactive)
                return;

            if (UIContainerBar.Instance.Target != null)
            {
                var device = UIContainerBar.Instance.Target.GetComponent<Device>();
                if (device != null)
                {
                    //this is the sequence to delete a decice
                    //WorldAnchorManager.Instance.removeAnchor(device.gameObject);
                    AppDataManager.Instance.sendDeleteOrderForWorldAnchorAndAppData(device.DeviceData);
                    //AppDataManager.Instance.removeDeviceFromAppDataAndDeleteFromHierarchie(device.DeviceData.baseAdress);

                }
            }

        }
    }
}