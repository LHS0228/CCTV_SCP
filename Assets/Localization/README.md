# 게임 다국어 지원

지원 언어는 한국어(`ko`), 영어(`en`), 일본어(`ja`), 중국어 간체(`zh-CN`), 중국어 번체(`zh-TW`)입니다. 첫 실행은 한국어이며 설정 메뉴에서 선택한 언어는 다음 실행에도 유지됩니다. 음성은 기존 녹음 파일을 사용합니다.

게임 중 **Esc → 언어 선택** 버튼을 누르면 다섯 언어가 펼쳐집니다. 버튼에 현재 언어가 표시되며, 선택 즉시 문구와 폰트가 바뀝니다. 타이틀 설정에서도 같은 버튼을 사용할 수 있습니다.

## 적용 범위

- 빌드에 포함된 타이틀, 게임, 엔딩 씬과 이 씬들이 참조하는 UI 프리팹
- 설정 및 종료 확인, 고용 계약서, 일차/클리어/보고서 표시
- 업무/개체/신체 이상 설명서와 프로토콜 안내
- 태블릿 미니게임 안내, 1~5일차 안내 자막, 엔딩 자막
- 일본어·중국어 타이틀 버튼: 이미지에 포함된 영문 대신 TMP 텍스트와 UI 테두리를 표시
- 태블릿 미니게임 시작 버튼: 모든 언어에서 기존 이미지 사용

문서의 한국어 서식과 타이틀 버튼 이미지는 유지합니다. 카메라 번호, 개체 식별 코드, 비밀번호, 수치는 언어와 관계없이 유지합니다. 아나운서 자막 10개는 제공받은 한국어 문장과 영어 녹음 원문을 기준으로 연결했습니다. 비상 안내는 프로토콜 시작 시, 마지막 관리자 안내는 엔딩 마지막 이벤트에서 재생됩니다.

## 번역 수정

`Assets/Localization/translations.json`이 번역 원본입니다. `key`는 고정 식별자, `ko/en/ja/zhCN/zhTW`는 각 언어 문구, `sources`는 기존 씬·프리팹 문구와 연결할 때 사용하는 값입니다. `literals`에는 번역하지 않는 식별자와 개발용 자리표시자가 들어 있습니다.

1. 원하는 언어의 문구를 수정합니다. `{0}` 같은 자리표시자와 TMP 태그를 유지합니다.
2. Unity에서 **Tools → Localization → Apply Translation Catalog**를 실행합니다.
3. **Tools → Localization → Validate Translations**로 검사합니다.
4. 실제 화면에서 잘림, 글자 크기, 읽는 시간과 표현을 확인합니다.

생성된 Unity String Table은 `Assets/Resources/Localization/Tables`에 있습니다. Localization Tables 창에서도 볼 수 있습니다. 생성된 표만 수정한 경우에는 JSON 원본에도 같은 수정을 반영해야 다음 적용에서 덮어쓰지 않습니다.

새 문구에는 `LocalizedTMPText` 컴포넌트를 추가하고 Key를 지정합니다. 코드에서 숫자 등이 바뀌는 문구는 아래처럼 연결합니다.

```csharp
GameLocalization.SetText(label, "ui.dayReport", currentDay);
```

이 연결은 화면을 보고 있는 도중 언어를 바꿔도 숫자를 유지하면서 문구와 폰트를 갱신합니다. 언어 변경은 `GameLocalization.SetLanguage(index)`로 처리합니다. 순서는 한국어, 영어, 일본어, 중국어 간체, 중국어 번체입니다.

## 구성

Unity Localization 1.5.13의 Locale과 String Table을 사용합니다. Locale은 설정 자산에 직접 참조하고 표와 폰트는 Resources에 포함합니다. `ResourceStringTableProvider`는 표 이름 `GameText`를 사용하는 Unity Localization API에도 같은 표를 제공합니다. 게임 실행 중 별도의 번역 서버나 Addressables 콘텐츠 다운로드는 필요하지 않습니다.

일본어와 중국어 간체·번체는 각각 Noto Sans CJK JP/SC/TC를 사용합니다. 배포 라이선스는 `Assets/Font/Localization/OFL.txt`와 `Assets/ThirdPartyNotices.txt`에 포함되어 있습니다. 영어 문서는 NanumMyeongjo-Regular를 사용하며, 다른 영어 UI는 기존 폰트에 필요한 글자가 있으면 그 폰트를 사용합니다. 외국어 문서에는 굵게 표시하는 태그와 Bold 스타일을 적용하지 않습니다. 외국어 문구에는 자동 크기 조정을 적용합니다.

기본 관리 규칙 페이지는 `ManualRulesLocalizationLayout`이 세 본문을 위에서 아래로 배치합니다. 세 항목에 같은 글자 크기를 사용하고, 문서 전체가 들어가는 범위에서 가장 큰 크기를 선택합니다. 영어·일본어의 붉은 프로토콜 안내는 크기를 줄이고 코드 숫자와 별도 영역에 표시합니다. 한국어로 바꾸면 원래 위치와 영역을 복구합니다. **Apply Translation Catalog**에 이 연결이 포함되어 있으며, 레이아웃만 다시 연결하려면 **Tools → Localization → Apply Manual Rules Layout**을 사용합니다.

번체 문구에는 `滑鼠`, `解析度`, `全螢幕` 등 대만에서 사용하는 UI 표현을 반영했습니다. 숫자로 입력하는 게임 코드는 프로그래밍 코드를 뜻하는 `程式碼` 대신 `代碼`로 표기합니다.

번역표의 글자는 정적 TMP 폰트 아틀라스에 미리 포함합니다. 번역을 바꿔 새 글자가 추가되면 **Apply Translation Catalog**를 다시 실행해야 합니다. 폰트 자산의 GUID와 기존 머티리얼 참조는 유지됩니다.

## 검수할 표현

| 한국어 원문 | 영어 | 일본어 | 중국어 간체 | 중국어 번체 |
|---|---|---|---|---|
| 촉수 군집/군체 | Tentacle Colony | 触手群体 | 触手群落 | 觸手群落 |
| 속박된 회로 | Bound Circuit | 束縛された回路 | 受缚电路 | 受縛電路 |
| 모방자 | Mimic | 模倣者 | 模仿者 | 模仿者 |
| 말소 | Erasure | 抹消 | 抹除 | 抹除 |
| 안정수치/안정도 | Stability | 安定度 | 稳定值 | 穩定值 |
| 시스템 검진/점검 | System Check | システム点検 | 系统检查 | 系統檢查 |

고유명사와 회사의 냉정한 말투는 번역 초안이므로 게임 의도에 맞는지 검수해야 합니다. 원본 프리팹에 남아 있는 개발용 예시와 테스트 설명도 번역 대상으로 연결했지만, 실제 빌드 씬에서 덮어쓴 정식 설명을 우선합니다. 긴 설명서·계약서와 8~10초 안내 자막은 실제 플레이에서 가독성을 확인해야 합니다.

## 자동 검사

Unity 6000.0.35f1에서 번역 누락, 표와 원본의 일치, 폰트 글자 지원, 빌드 씬의 문구 연결 및 언어 선택 UI를 검사합니다. `GameLocalizationPlayModeValidation.Run`은 세 씬의 실제 Play Mode에서 언어 선택 콜백, 선택 저장, 동적 문구 갱신, 기본 Unity 문자열 API와 타이틀 버튼의 클릭 가능 여부를 검사합니다. 계약서 본문의 최소 크기, 넘침, 제목·동의 질문과의 간격, 한국어 복귀도 검사합니다. 검사가 끝나면 기존 언어 선택값과 열린 씬을 복구합니다.

고용 계약서의 `LocalizedTMPText`에는 동의 질문을 `Translated Bottom Boundary`로 연결하고 `Minimum Translated Size Ratio`를 0.75로 지정합니다. 번역 본문은 질문 위까지 영역을 넓혀 표시하고, 한국어는 원래 레이아웃을 복구합니다. `Apply Translation Catalog`를 다시 실행해도 이 설정이 유지됩니다.

자동 검사는 전체 게임 진행이나 원어민 검수를 대체하지 않습니다. 검증 로그와 임시 Windows 빌드는 Git에서 제외된 `.utmp/localization` 폴더에 저장됩니다.

## Editor 런타임 테스트

**Tools → Testing → Runtime Test**에서 현재 날짜·시간을 확인하고 시간 변경, 06:00 이동, 원하는 이상현상 즉시 실행을 사용할 수 있습니다. 게임 씬의 Play Mode에서 활성화됩니다. 자세한 사용법은 `Assets/Editor/RuntimeTesting/README.md`를 참고하세요. 창과 UI 파일은 Editor 폴더에 있고 게임 쪽 테스트 API도 `UNITY_EDITOR`로 감싸져 있어 Player 빌드에는 포함되지 않습니다.
