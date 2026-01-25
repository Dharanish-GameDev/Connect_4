using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class NumberButton : MonoBehaviour
{
   [SerializeField] private TextMeshProUGUI numberText;
   
   [SerializeField] private Button button;

   [SerializeField] private GameObject selectedVisual;
   [SerializeField] private GameObject unselectedVisual;
   
   int number;
   
   public int Number => number;

   public void ConfigureButton(int number, Action onClickEvent)
   {
       this.number = number;
       numberText.text = number.ToString();
       button.onClick.AddListener(() => onClickEvent?.Invoke());
   }

   public void Select()
   {
       selectedVisual.SetActive(true);
       unselectedVisual.SetActive(false);
   }

   public void Deselect()
   {
       selectedVisual.SetActive(false);
       unselectedVisual.SetActive(true);
   }
}
