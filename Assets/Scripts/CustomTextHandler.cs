using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CustomTextHandler : OVRVirtualKeyboard.AbstractTextHandler
{
    public InputField inputField; // Riferimento all'InputField
    public GameObject errorTextField;
    public GameObject infoTextField;

    public Action<string> OnSubmitValidNumber; // Callback per inviare il testo se valido

    public override Action<string> OnTextChanged { get; set; }

    public override string Text
    {
        get { return inputField.text; }
    }

    public override bool SubmitOnEnter
    {
        get { return true; } // Imposta su true se vuoi inviare il testo premendo invio
    }

    public override bool IsFocused
    {
        get { return inputField.isFocused; }
    }

    private void Start()
    {
        if (inputField == null)
        {
            Debug.LogError("InputField not assigned in the inspector");
        }
    }

    public override void Submit()
    {
        if (IsNumber(Text))
        {
            Debug.Log("Submitted text: " + Text);
            errorTextField.GetComponent<TextMeshProUGUI>().enabled = false; // Nasconde il messaggio di errore
            infoTextField.GetComponent<TextMeshProUGUI>().enabled = true;

            // Chiama la callback solo se il numero è valido
            OnSubmitValidNumber?.Invoke(Text);
        }
        else
        {
            Debug.LogError("Input is not a number.");
            errorTextField.GetComponent<TextMeshProUGUI>().enabled = true; // mostra il messaggio di errore
            infoTextField.GetComponent<TextMeshProUGUI>().enabled = false;
            inputField.text = ""; // Resetta il campo di input
        }
    }

    private bool IsNumber(string text)
    {
        // Tenta di convertire il testo in un numero
        return int.TryParse(text, out _);
    }

    public override void AppendText(string s)
    {
        inputField.text += s;
        OnTextChanged?.Invoke(inputField.text);
    }

    public override void ApplyBackspace()
    {
        if (inputField.text.Length > 0)
        {
            inputField.text = inputField.text.Substring(0, inputField.text.Length - 1);
            OnTextChanged?.Invoke(inputField.text);
        }
    }

    public override void MoveTextEnd()
    {
        inputField.caretPosition = inputField.text.Length;
    }
}
