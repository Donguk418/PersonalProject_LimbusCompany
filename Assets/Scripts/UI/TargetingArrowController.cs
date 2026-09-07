using System.Collections.Generic;
using UnityEngine;
using Limbus.Runtime;

namespace Limbus.Runtime
{
    public class TargetingArrowController : MonoBehaviour
    {
        [Header("선 렌더링 설정")]
        [SerializeField] private LineRenderer _dragLineRenderer;                                // 드래그 전용 임시 선
        [SerializeField] private LineRenderer _arrowPrefab;                                     // 확정된 타깃팅 화살표 프리팹
        [SerializeField] private Transform _linesContainer;                                     // 생성된 선들을 모아둘 부모 트랜스폼
        [SerializeField] private int _curveSegmentCount = 25;                                   // 곡선 세그먼트 개수
        [SerializeField] private float _curveHeightFactor = 100f;                               // 곡률 높이 계수

        private Canvas _canvas;
        private Camera _uiCamera;

        private SkillSlotUI _currentDraggingSlot;
        private readonly Dictionary<SkillSlotUI, LineRenderer> _activeLines = new();

        private void Awake()
        {

            _canvas = GetComponentInParent<Canvas>();                                            // 상위 캔버스 탐색 및 UI 전용 카메라 캐싱
            if (_canvas != null)
            {
                _uiCamera = _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
            }
        }

        private void OnEnable()
        {
            SkillSlotUI.OnSkillDragStarted += HandleSkillDragStarted;
            SkillSlotUI.OnSkillDragging += HandleSkillDragging;
            SkillSlotUI.OnSkillDragEnded += HandleSkillDragEnded;
            EnemySlotUI.OnSlotTargetAssigned += HandleSlotTargetAssigned;
        }

        private void OnDisable()
        {
            SkillSlotUI.OnSkillDragStarted -= HandleSkillDragStarted;
            SkillSlotUI.OnSkillDragging -= HandleSkillDragging;
            SkillSlotUI.OnSkillDragEnded -= HandleSkillDragEnded;
            EnemySlotUI.OnSlotTargetAssigned -= HandleSlotTargetAssigned;
        }

        private void Start()
        {
            if (_dragLineRenderer != null)
            {
                _dragLineRenderer.positionCount = _curveSegmentCount;
                _dragLineRenderer.enabled = false;
            }
        }

        private void HandleSkillDragStarted(SkillSlotUI slot, int skillIndex, Vector2 screenPos)
        {
            _currentDraggingSlot = slot;

            if (_dragLineRenderer != null)
            {
                _dragLineRenderer.enabled = true;
                Vector3 mouseWorldPos = ScreenToWorldPoint(screenPos, slot.transform.position.z);
                UpdateLineCurve(_dragLineRenderer, slot.transform.position, mouseWorldPos);
            }
        }

        private void HandleSkillDragging(Vector2 screenPos)
        {
            if (_currentDraggingSlot == null || _dragLineRenderer == null)
            {
                return;
            }

            Vector3 mouseWorldPos = ScreenToWorldPoint(screenPos, _currentDraggingSlot.transform.position.z);
            UpdateLineCurve(_dragLineRenderer, _currentDraggingSlot.transform.position, mouseWorldPos);
        }

        private void HandleSkillDragEnded(SkillSlotUI slot, GameObject droppedTarget)
        {
            _currentDraggingSlot = null;

            if (_dragLineRenderer != null)
            {
                _dragLineRenderer.enabled = false;
            }
        }

        private void HandleSlotTargetAssigned(SkillSlotUI playerSlot, EnemySlotUI enemySlot)
        {
            if (playerSlot == null || enemySlot == null)
            {
                return;
            }

            ClearLine(playerSlot);                                      // 슬롯 타겟 재지정 시 기존 화살표 제거

            LineRenderer newLine = Instantiate(_arrowPrefab, _linesContainer != null ? _linesContainer : transform);
            newLine.positionCount = _curveSegmentCount;
            newLine.enabled = true;

            UpdateLineCurve(newLine, playerSlot.transform.position, enemySlot.ArrowAnchor.position);

            _activeLines[playerSlot] = newLine;
        }

        private Vector3 ScreenToWorldPoint(Vector2 screenPos, float targetZ)
        {
            RectTransform canvasRect = _canvas != null ? _canvas.GetComponent<RectTransform>() : null;

            if (canvasRect != null && RectTransformUtility.ScreenPointToWorldPointInRectangle(canvasRect, screenPos, _uiCamera, out Vector3 worldPoint))
            {
                worldPoint.z = targetZ;
                return worldPoint;
            }

            Vector3 fallback = Input.mousePosition;
            fallback.z = targetZ;
            return fallback;
        }

        public void ClearLine(SkillSlotUI playerSlot)
        {
            if (playerSlot == null)
            {
                return;
            }

            if (_activeLines.TryGetValue(playerSlot, out LineRenderer line))
            {
                if (line != null)
                {
                    Destroy(line.gameObject);
                }

                _activeLines.Remove(playerSlot);
            }
        }

        public void ClearAllLines()
        {
            foreach (var kvp in _activeLines)
            {
                if (kvp.Value != null)
                {
                    Destroy(kvp.Value.gameObject);
                }
            }

            _activeLines.Clear();
        }

        private void UpdateLineCurve(LineRenderer line, Vector3 startWorldPos, Vector3 endPos)
        {
            endPos.z = startWorldPos.z;

            Vector3 midPoint = (startWorldPos + endPos) * 0.5f;
            midPoint.y += _curveHeightFactor;

            for (int i = 0; i < _curveSegmentCount; i++)
            {
                float t = i / (float)(_curveSegmentCount - 1);
                Vector3 curvePoint = CalculateBezierPoint(t, startWorldPos, midPoint, endPos);
                line.SetPosition(i, curvePoint);
            }
        }

        private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2)
        {
            float u = 1f - t;
            return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
        }
    }
}