using HoloToolkit.Unity.Buttons;
using System.Collections;
using System.Collections.Generic;
using ThesisHololens.States;
using ThesisHololens.UI;
using UnityEngine;

public class UIPlayer : UIContainer {

    [SerializeField]
    private CompoundButton Prev;
    [SerializeField]
    private CompoundButton Play;
    [SerializeField]
    private CompoundButton Pause;
    [SerializeField]
    private CompoundButton Next;


    // Use this for initialization
    void Start()
    {
        Prev.OnButtonClicked += Prev_OnButtonClicked;
        Play.OnButtonClicked += Play_OnButtonClicked;
        Pause.OnButtonClicked += Pause_OnButtonClicked;
        Next.OnButtonClicked += Next_OnButtonClicked;

    }

    private void Next_OnButtonClicked(GameObject obj)
    {
        if (!interactionAllowed())
            return;

        ItemStates.Instance.updateItem(adress, "NEXT", true);
    }

    private void Pause_OnButtonClicked(GameObject obj)
    {
        if (!interactionAllowed())
            return;

        ItemStates.Instance.updateItem(adress, "PAUSE", true);
    }

    private void Play_OnButtonClicked(GameObject obj)
    {
        if (!interactionAllowed())
            return;

        ItemStates.Instance.updateItem(adress, "PLAY", true);
    }

    private void Prev_OnButtonClicked(GameObject obj)
    {
        if (!interactionAllowed())
            return;

        ItemStates.Instance.updateItem(adress, "PREVIOUS", true);
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

        Prev.OnButtonClicked -= Prev_OnButtonClicked;
        Play.OnButtonClicked -= Play_OnButtonClicked;
        Pause.OnButtonClicked -= Pause_OnButtonClicked;
        Next.OnButtonClicked -= Next_OnButtonClicked;

    }




}
