# ORIGIN UI/UX 자체 체크리스트 V1.2

> `Tools > Game UI > ORIGIN UI > 7. Validate Portfolio Requirements`는 구조를 자동 점검한다. 아래의 실제 결과 칸은 직접 플레이 테스트 후 작성한다.

| 확인 항목 | 기대 결과 | 실제 결과 | 수정 내용 | 재확인 |
|---|---|---|---|---|
| Hub 씬 | TIME/HUNT가 보이지 않는다 |  |  |  |
| Dungeon 씬 | TIME/HUNT가 자동으로 나타난다 |  |  |  |
| 1920x1080 | HUD가 기준 위치에 배치된다 |  |  |  |
| 다른 16:9 해상도 | Anchor가 유지되어 화면 밖으로 나가지 않는다 |  |  |  |
| 다른 화면 비율 | 중요 UI가 잘리거나 겹치지 않는다 |  |  |  |
| HP 데이터 | F6/F7 시 숫자와 게이지가 함께 변경된다 |  |  |  |
| 시간 데이터 | Dungeon에서만 타이머가 감소한다 |  |  |  |
| Pause 시간 | ESC Pause 중 타이머가 멈춘다 |  |  |  |
| 목표 안내 | Objective 텍스트가 읽기 쉽다 |  |  |  |
| 처치 진행 | F8 입력 시 HUNT 값이 갱신된다 |  |  |  |
| Pause | ESC로 열고 닫을 수 있다 |  |  |  |
| Mouse | 메뉴 버튼 클릭 가능 |  |  |  |
| Keyboard Move | 방향키/WASD로 메뉴 선택 이동 가능 |  |  |  |
| Submit | Enter로 선택 버튼 실행 가능 |  |  |  |
| Cancel | Esc로 메뉴 취소/일시정지 가능 |  |  |  |
| Toast | F9 입력 시 나타났다가 사라진다 |  |  |  |
| Interaction | F10 입력 시 프롬프트가 켜지고 꺼진다 |  |  |  |
| Prefab | MenuButton/GaugeBar/ToastMessage가 존재한다 |  |  |  |

## 필수 수정 전/후 기록 예시
### UI 요소 1 - DungeonInfo
- 기대 결과: 던전에서만 표시
- 최초 결과:
- 수정 내용: 씬 자동 감지 적용
- 재확인 결과:

### UI 요소 2 - 해상도/버튼/목표 패널 중 하나
- 기대 결과:
- 최초 결과:
- 수정 내용:
- 재확인 결과:

## 제출 전 체크
- [ ] UGUI와 TextMeshPro 사용
- [ ] UI/UX 콘셉트와 UI 리소스 규칙 기록
- [ ] GUI 디자인 가이드 및 구현 가능성 검토 기록
- [ ] Canvas Scaler 기준 해상도 기록
- [ ] HUD 3종 이상 실제 스크립트 변수 연결
- [ ] Title/HUD/Pause/Result 중 3화면 이상 구현
- [ ] 메뉴 버튼으로 화면 전환 확인
- [ ] 마우스 클릭 확인
- [ ] 키보드 메뉴 이동 / Submit / Cancel 중 2개 이상 실제 확인
- [ ] 재사용 Prefab 2개 이상
- [ ] UI 피드백 2개 이상
- [ ] 해상도 변경 테스트
- [ ] UI 요소 2개 이상 수정 전/후 기록
- [ ] 실행 화면 캡처 2장 이상
