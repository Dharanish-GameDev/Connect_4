using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NumbersHandler : MonoBehaviour
{


    [SerializeField] private NumberButton buttonRef;

    [SerializeField] private RectTransform buttonContainer;
    [SerializeField] private RectTransform firstButtonContainer;
    
    private int currentValue = -1;
    
    [SerializeField] private int maxValue = 100;
    
    private List<NumberButton> numberButtons = new List<NumberButton>();
    
    [SerializeField] private Button submitButton;
    
    public int CurrentValue => currentValue;
    
    private static NumbersHandler instance;
    
    public static NumbersHandler Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NumbersHandler>(FindObjectsInactive.Include);
            }

            if (instance == null)
            {
                Debug.LogError("NumbersHandler instance not initialized! Make sure it's in the scene.");
            }
            return instance;
        }
    }
    
    private void Awake()
    {
        InitializeSingleton();
        PopulateButtons();
    }
    
    private void InitializeSingleton()
    {
        // First check: Is there already an instance?
        if (instance != null && instance != this)
        {
            Debug.LogWarning($"Multiple NumbersHandler instances found. Destroying {gameObject.name}");
            Destroy(gameObject);
            return;
        }
        
        // Set this as the instance
        instance = this;
        
        // Optional persistence
        DontDestroyOnLoad(gameObject);
        
        Debug.Log($"NumbersHandler initialized: {gameObject.name}");
    }
    
    private void PopulateButtons()
    {
        NumberButton zeroButton = Instantiate(buttonRef, firstButtonContainer);
        if (zeroButton != null)
        {
            numberButtons.Add(zeroButton);
            zeroButton.transform.localPosition = Vector3.zero;
            zeroButton.ConfigureButton(0, () =>
            {
                OnNumberClicked(zeroButton);
            });
        }
        for (int i = 1; i <= maxValue; i++)
        {
            NumberButton button = Instantiate(buttonRef, buttonContainer);
            button.ConfigureButton(i, () =>
            {
                OnNumberClicked(button);
            });
            numberButtons.Add(button);
        }
    }

    private void OnNumberClicked(NumberButton button)
    {
        currentValue = button.Number;
        SelectButton(button);
        Debug.Log(button.Number + " Clicked");
    }

    private void SelectButton(NumberButton button)
    {
        for (int i = 0; i < numberButtons.Count; i++)
        {
            if (numberButtons[i] == button) numberButtons[i].Select();
            else  numberButtons[i].Deselect();
        }
    }

    public void ResetValues()
    {
        currentValue = 0;
        for (int i = 0; i < numberButtons.Count; i++)
        {
            numberButtons[i].Deselect();    
        }
        submitButton.interactable = true;
    }

    public void SetSubmitButtonEvent(Action onSubmitButtonClicked)
    {
        if(submitButton == null) return;
        submitButton.onClick.AddListener(() =>
        {
            onSubmitButtonClicked?.Invoke();
            submitButton.interactable = false;
        });
    }
}
