using UnityEngine;

namespace Match3Wizard
{
	public class PieceTapHandler : MonoBehaviour
	{
		private void OnMouseDown()
		{
			BoardManager.Instance.OnPieceTapped(GetComponent<BoardPiece>());
		}
	}
}