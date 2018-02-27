using UnityEngine;
using System.Collections;

// Component that controls the movement and properties of the camera
public class CameraController : MonoBehaviour {

	public float mouseEdgeCameraMoveSize;
	public float moveSpeed;
	public float dragSpeed;
	public float rightButtonDeadzone;

	public float minZoom;
	public float maxZoom;

	Vector3 positionChange;

	MapController mapController;
	Camera UICamera;

	Vector2 initialMousePosition;

	public void MoveTo(Vector3 location) {
		float posY = transform.position.y;
		transform.position = new Vector3(location.x, posY, location.y);
		transform.Translate(Vector3.back * posY * 1.2f);
		transform.position = new Vector3(transform.position.x, posY, transform.position.z);
	}

	void Awake() {
		mapController = FindObjectOfType<MapController>();
		UICamera = GameObject.FindGameObjectWithTag("UICamera").GetComponent<Camera>();
	}

	void Start() {
		float posY = transform.position.y;
		transform.position = new Vector3(mapController.map.VolcanoX, posY, mapController.map.VolcanoY);
		transform.Translate(Vector3.back * posY * 1.2f);
		transform.position = new Vector3(transform.position.x, posY, transform.position.z);
	}

	void Update() {
		if(GameController.instance.CurrentGameState == GameController.GameState.Game) {
			positionChange = new Vector3();
			float thisMoveSpeed = moveSpeed * Time.fixedDeltaTime * Camera.main.orthographicSize;

			Vector2 mousePosition = (Vector2)Input.mousePosition;
			if(mousePosition.x < Screen.width - rightButtonDeadzone || mousePosition.y > GameController.instance.UIHeight) {
				if(mousePosition.x < mouseEdgeCameraMoveSize && mousePosition.x > 0) {
					positionChange.x = -thisMoveSpeed / 2;
				} else if(mousePosition.x > Screen.width - mouseEdgeCameraMoveSize && mousePosition.x < Screen.width) {
					positionChange.x = thisMoveSpeed / 2;
				}

				if(mousePosition.y < mouseEdgeCameraMoveSize && mousePosition.y > 0) {
					positionChange.z = -thisMoveSpeed;
				} else if(mousePosition.y > Screen.height - mouseEdgeCameraMoveSize && mousePosition.y < Screen.height) {
					positionChange.z = thisMoveSpeed; 
				}

				if(Input.GetMouseButtonDown(2)) {
					initialMousePosition = mousePosition;
				}

				Vector2 deltaMouse = (mousePosition - initialMousePosition) * dragSpeed;
				if(Input.GetMouseButton(2)) {
					positionChange = new Vector3(positionChange.x + deltaMouse.x / 2, positionChange.y, positionChange.z + deltaMouse.y);
				}

				Camera.main.orthographicSize = Mathf.Clamp(Camera.main.orthographicSize - Input.GetAxis("Mouse ScrollWheel"), minZoom, maxZoom);
				UICamera.orthographicSize = Camera.main.orthographicSize;

				float cameraY = transform.position.y;
				transform.Translate(positionChange);
				transform.position = new Vector3(transform.position.x, cameraY, transform.position.z);
			}
		}
	}

}
