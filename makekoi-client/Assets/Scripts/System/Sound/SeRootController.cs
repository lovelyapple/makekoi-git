using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SeRootController : MonoBehaviour
{
    [SerializeField] SeController SePrefab;
    private List<SeController> SeControllers = new List<SeController>();
    private const int MaxSeCount = 30;

    public void PlaySe(AudioClip clip)
    {
        var empty = SeControllers.FirstOrDefault(x => x.IsEmpty);

        if (empty != null)
        {
            empty.PlaySe(clip);
            return;
        }

        if (SeControllers.Count >= MaxSeCount)
        {
            Debug.LogWarning($"max se count");
            return;
        }

        var newOne = Instantiate(SePrefab, this.transform).GetComponent<SeController>();
        newOne.PlaySe(clip);
        SeControllers.Add(newOne);
    }
}
