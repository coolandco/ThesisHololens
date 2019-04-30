using HoloToolkit.Unity.Buttons;
using System.Collections;
using System.Collections.Generic;
using ThesisHololens.States;
using ThesisHololens.UI;
using UnityEngine;

public class UIRollershutter : UIContainer {

    [SerializeField]
    private CompoundButton Up;
    [SerializeField]
    private CompoundButton Stop;
    [SerializeField]
    private CompoundButton Down;
    // Use this for initialization
    void Start()
    {
        Up.OnButtonClicked += Up_OnButtonClicked;
        Stop.OnButtonClicked += Stop_OnButtonClicked;
        Down.OnButtonClicked += Down_OnButtonClicked;

    }

    private void Down_OnButtonClicked(GameObject obj)
    {
        if (!interactionAllowed())
            return;

        ItemStates.Instance.updateItem(adress, "DOWN", true);
    }

    private void Stop_OnButtonClicked(GameObject obj)
    {
        if (!interactionAllowed())
            return;
        ItemStates.Instance.updateItem(adress, "STOP", true);
    }

    private void Up_OnButtonClicked(GameObject obj)
    {
        if (!interactionAllowed())
            return;

        ItemStates.Instance.updateItem(adress, "UP", true);
    }

    protected override void onManipulationEnd_internal()
    {
    }

    protected override void onManipulationStart_internal()
    {
    }

    protected override void stateChanged_internal(string state)
    {
    }

    private void OnDestroy()
    {

        Up.OnButtonClicked -= Up_OnButtonClicked;
        Stop.OnButtonClicked -= Stop_OnButtonClicked;
        Down.OnButtonClicked -= Down_OnButtonClicked;

    }



}
