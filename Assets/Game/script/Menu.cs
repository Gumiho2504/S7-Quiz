using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
public class Menu : MonoBehaviour
{
    public RectTransform logo; 
    public float scaleFactor = 1.5f; 
    public float duration = 1f; 
    public float rotationAmount = 360f; 
    public GameObject categoryPanel,loadingpanel;
    public Button[] categoryButtons;


    public Text loadingText; 
    public float dotDelay = 0.5f;  
    private string baseText = "Loading"; 
    private int dotCount = 0;
    private int maxDots = 3;

    IEnumerator Start()
    {
        AnimateLoadingText();
        yield return new WaitForSeconds(2f);
        LeanTween.moveLocalY(loadingpanel, Screen.height, 0.5f).setEaseInBack().setOnComplete(() => {
            loadingpanel.SetActive(false);
        });
        yield return new WaitForSeconds(0.5f);
        logo.localScale = Vector3.one;
        AnimateLogo();


        categoryPanel.transform.localPosition = new Vector3(0, Screen.height, 0);
        LeanTween.moveLocalY(categoryPanel, 0f, 0.5f).setEaseOutBack();
        foreach (Button categoryButton in categoryButtons)
        {
            int buttonIndex = System.Array.IndexOf(categoryButtons, categoryButton);
            categoryButton.transform.localScale = Vector3.zero;
            LeanTween.scale(categoryButton.gameObject, Vector3.one, 0.5f).setEaseOutBounce().setDelay(0.2f * buttonIndex);
        }
    }

    public void StartPlay()
    {
        LeanTween.moveLocalY(categoryPanel, Screen.height, 0.5f).setEaseInBack().setOnComplete(() => {
            SceneManager.LoadScene("Menu");
        });
        
    }

    public void Quit()
    {
        Application.Quit();
    }

    void AnimateLogo()
    {
      
        LeanTween.scale(logo, Vector3.one * scaleFactor, duration).setEase(LeanTweenType.easeInOutQuad)
            .setOnComplete(() => {
               
                LeanTween.scale(logo, Vector3.one, duration).setEase(LeanTweenType.easeInOutQuad)
                    .setOnComplete(() => {
                      
                        LeanTween.rotateZ(logo.gameObject, rotationAmount, duration * 2).setEase(LeanTweenType.easeInOutQuad)
                            .setOnComplete(() => {
                              
                                LeanTween.alpha(logo, 0f, duration).setEase(LeanTweenType.easeInOutQuad)
                                    .setOnComplete(() => {
                                        LeanTween.alpha(logo, 1f, duration).setEase(LeanTweenType.easeInOutQuad)
                                            .setOnComplete(AnimateLogo); // Repeat the whole animation
                                    });
                            });
                    });
            });
    }

    void AnimateLoadingText()
    {
      
        dotCount = (dotCount + 1) % (maxDots + 1);

       
        loadingText.text = baseText + new string('.', dotCount);

      
        LeanTween.delayedCall(gameObject, dotDelay, AnimateLoadingText);
    }
}
