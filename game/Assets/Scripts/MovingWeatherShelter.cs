using UnityEngine;
// Train roofs re-register only while moving. The existing static roof index remains unchanged.
public sealed class MovingWeatherShelter : MonoBehaviour {
    WeatherShelter shelter;
    Vector3 registered;
    void Awake(){shelter=GetComponent<WeatherShelter>();registered=transform.position;}
    void LateUpdate(){if((registered-transform.position).sqrMagnitude<.04f)return;shelter.enabled=false;shelter.enabled=true;registered=transform.position;}
}
