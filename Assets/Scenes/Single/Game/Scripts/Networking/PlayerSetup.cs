// using UnityEngine;
// using Unity.Netcode;
// using System.Collections;

// public class PlayerSetup : NetworkBehaviour
// {
//     public TankSniperView sniperView;    // повесь ссылки на эти компоненты в префабе (скрипты на корне префаба)
//     public TurretAiming turretAiming;

//     public override void OnNetworkSpawn()
//     {
//         if (!IsOwner) return; // только локальный игрок настраивает свою камеру/UI

//         // попробуем получить GameUIManager. Если он ещё не готов — запустим корутину ожидания.
//         if (GameUIManager.Instance != null)
//         {
//             ApplyUI(GameNetUIManager.Instance);
//         }
//         else
//         {
//             StartCoroutine(WaitForUIAndApply());
//         }
//     }

//     IEnumerator WaitForUIAndApply()
//     {
//         float timeout = 5f;
//         float t = 0f;
//         while (GameUIManager.Instance == null && t < timeout)
//         {
//             t += Time.deltaTime;
//             yield return null;
//         }
//         if (GameUIManager.Instance != null)
//             ApplyUI(GameNetUIManager.Instance);
//         else
//             Debug.LogError("[PlayerSetup] GameUIManager not found in scene!");
//     }

//     void ApplyUI(GameNetUIManager ui)
//     {
//         sniperView?.InitializeForLocalPlayer(ui.mainCamera, ui.crosshairAimUI, ui.crosshairSniperUI, ui.sniperVignette);
//         turretAiming?.InitializeForLocalPlayer(ui.mainCamera != null ? ui.mainCamera.transform : null);
//     }
// }
