# QoL: Easy Reading — RimWorld 1.7 v1.1.0 / PORT17-V110-RELDIAG1

- **기준 프로젝트:** `QoLEasyReading 261117 1034 1.7.zip` (기존 1.7 성공 보고본)
- **차이 이식 기준:** `261117 1118.zip`의 1.6 S3-REL-DIAG1 소스
- **빌드 ID:** `ER-RW17-S3QC-HF1-20261117-R1`
- **공식 버전:** `1.1.0` / DLL AssemblyVersion: `1.1.0.0`
- **상태:** S3-QC-HF1 Quality Colors 툴팁 호환성 수정 소스 반영. **새 Debug/Release DLL 빌드 및 1.7 실게임 검증은 아직 필요.**

## S3-QC-HF1 — 2026-11-17

- 1.6 실게임에서 검증된 `SchematicDuplicateTooltipContent.cs` 제목 정리 로직을 1.7에 바이트 동일하게 이식했습니다.
- Quality Colors 등의 `<color=...>`와 정상적인 `b/i` 표시 태그만 제거하고 실제 품질명/제목은 보존합니다.
- 동일한 제목에 색상 태그가 달라도 `×N`으로 합치며, Debug 자체 검사 **54개**를 유지합니다.
- 외부 모드 DLL, 거래/연구 정책, Harmony 후킹, 1.7 빌드 참조, 기존 소유 판정은 수정하지 않았습니다.
- 기존 1.7 Debug/Release DLL은 모두 구버전이므로 반드시 **둘 다 Rebuild**하십시오.
- 신규 코드 기준 실제 1.7 실행 검증은 아직 수행되지 않았습니다.

## 무엇이 변경되었나

이미 작동하는 1.7의 Schematic/독서 기능 자체를 재작성하지 않았습니다. 최신 1.6의 엄격 Debug/Release 진단 경계만 이식했습니다.

| 구성 | 공통 기능 CS | 진단 전용 CS | 전체 CS | DebugAction 목표 | Diagnostics 설정 |
|---|---:|---:|---:|---:|---|
| Debug | 55 | 8 | **63** | **5** | 보임 |
| Release | 55 | 0 | **55** | **0** | 숨김 |

- `Core/EasyReadingRuntimeHealth.cs` 1개 추가. 생산용 연구 API 접근 실패 경고는 Release에서도 살아 있습니다.
- `SchematicOwnershipTrace.cs` 및 중복 보유 책 제목 툴팁, 거래 스냅샷 캐시는 Release에도 포함됩니다.
- Release의 `EasyReadingDebug.Enabled`는 기존 XML 설정값과 관계없이 `false`입니다.
- Debug 시작 검사는 유지하되 Release에서 순수 자체 검사를 실행하지 않습니다.
- `BookTradeSkillIcons` 4개 partial 타입과 Harmony 패치 시그니처는 유지했습니다.
- 1.7 기존 **SDK `net10.0`, Harmony 3, Waylith, UnityStubs 및 GUID**를 보존했습니다.
- `About/About.xml`: 사용자 이름은 반드시 **`QoL: Easy Reading`**, 설명에만 `Version: v1.1.0` 표기. 게임 공용 `About.xml`을 설치 도중 자동으로 덮어쓰지 않습니다.
- 1.7 기존 텍스처 17개 및 EN/KO 언어 XML 2개는 바이트 그대로 사용합니다. 1.6 공통 자산과 같은 경로의 파일 16개 PNG/2개 XML은 SHA가 동일합니다.

## Windows VS / .NET 10 빌드

1. 이 프로젝트를 **1.6과 다른 폴더**에 압축 해제합니다.
2. `.NET 10 SDK`를 설치하고 `RimWorld17.local.props.example`을 참고해 `RimWorld17.local.props`를 설정합니다. 기존 프로젝트의 상위 props가 이미 있으면 그대로 사용해도 됩니다.
3. **참조 DLL 4개**(`Assembly-CSharp.dll`, `UnityStubs.dll`, `Waylith.dll`, `0Harmony.dll`)를 게임 1.7 참조 폴더에 둡니다. **이 ZIP에는 포함하지 않았습니다.**
4. 프로젝트 폴더 PowerShell에서 실행합니다.

```powershell
.\BuildAndInstall_RW17.ps1 -BuildOnly -ReferencesDir 'D:\RimWorld17Refs\1.7'
```

자동 빌드 스크립트는 Debug, Release를 각각 **Rebuild**하고, 별도의 .NET 10 순수 메타데이터 검사기를 컴파일해 **Debug 5 / Release 0 개발자 액션**, `1.1.0.0` AssemblyVersion, `1.1.0` InformationalVersion, 빌드 ID를 DLL에서 검사합니다. 게임 DLL의 코드는 실행하지 않습니다.

검사 결과: `PORT17_LAST_BUILD_INSTALL_LOG.json`, `PORT17_BUILD_Debug.log`, `PORT17_BUILD_Release.log`, `PORT17_SOURCE_MANIFEST.json`.

### 1.7 게임에 설치

게임 종료 후, 실제 사용자 모드 루트 `About/About.xml`의 제목과 버전이 v1.1.0인지 확인합니다. 현재 1.6 모드의 공용 About.xml을 이미 업데이트했다면 **덮어쓰지 마세요**.

```powershell
# Debug 검증용 DLL
.\BuildAndInstall_RW17.ps1 -ModRoot 'D:\RimWorld\Mods\QoLEasyReading' -ReferencesDir 'D:\RimWorld17Refs\1.7' -InstallDebug

# Release 배포 검증용 DLL (기본값)
.\BuildAndInstall_RW17.ps1 -ModRoot 'D:\RimWorld\Mods\QoLEasyReading' -ReferencesDir 'D:\RimWorld17Refs\1.7'
```

**설치 범위:** `1.7/Assemblies/QoLEasyReading.dll` + 공용 PNG/번역 파일. 기존 `1.6/Assemblies/QoLEasyReading.dll` 및 `About/About.xml`·`About/PublishedFileId.txt`는 변경하지 않습니다. 대상별 백업/해시 검증·실패 복원 기능이 있습니다.

## 실게임 검증 순서

- Debug 시작 로그: `BUILD_ID=ER-RW17-S3QC-HF1-20261117-R1`, `MOD_VERSION=1.1.0`, `configuration=Debug`, 개발자 진단 5개
- Debug 진단: 연구 11, 소유 17, 구매 정책 14, 툴팁 54, E/F/G 31, 실제 거래 세션 롤오버
- 기본 독서, Shift, Ctrl 반복, Alt 대화상자, Work 시간 수동 독서, 책장 아이콘과 설정 탭
- 거래: `? / ! / ✓` 아이콘·중복 보유 제목 툴팁·판매자/플레이어 방향·새 거래에서 보유 스냅샷 갱신
- Release 시작 로그: `configuration=Release`, 개발자 액션 0개, 설정 Diagnostics 숨김, 예전 XML `enableDebugLogging=true`여도 상세 로그 실행 없음
- Release 동일 기능 스모크 및 Harmony 예외 0 확인, 필요하면 타 모드와 함께 테스트
- 완료한 최종 Release DLL의 SHA-256을 기록해 **같은 DLL**을 배포

**미검증 경계:** 1.6의 `UNIFORM_CANDIDATE_DIRECTION_UNVERIFIED`와 타 모드 전용 보관함 스캔 한계는 변경하지 않았습니다. 1.7에서 작동했다는 사용자 보고는 귀중하지만 이 새 Release DLL의 검증 증거를 대신하지 않습니다.

## 프로젝트 구조 / 리팩토링

`Books/`, `Core/`, `Diagnostics/`, `RepeatReading/`, `Settings/`, `Schematic/`, `Properties/`, `Languages/`, `Textures/` 유지. 신규 생산 CS는 `Core/EasyReadingRuntimeHealth.cs`뿐이며, 별도 진단 검증기 소스는 `Port17_Reports/PORT17_V110_RELDIAG1/DevActionVerifier` 아래에 둡니다. 게임플레이 CS를 재분할하지 않습니다.

**중요:** 기존 Debug DLL이나 `bin/obj`는 ZIP에 넣지 않았으므로 반드시 **Rebuild**가 필요합니다. 이번 작업은 **1.7 프로젝트만 수정**했으며, 별도로 발견된 1.6 설치 스크립트/PNG 문제는 해결됐다고 주장하지 않습니다.
