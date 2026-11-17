# QoL: Easy Reading — v1.1.0 (Quality Colors 호환성 수정)

제작 **ZCN** · **소스 프로젝트:** RimWorld **1.6 + 1.7** · **현재 다운로드용 Release DLL:** RimWorld **1.7 전용**

[Steam 창작마당](https://steamcommunity.com/sharedfiles/filedetails/?id=3828178548) · [변경 내역](CHANGELOG.md) · [현재 배포 상태](PACKAGING_STATUS.md)

## 핵심 기능

- 책장·독서 우클릭 메뉴·거래창에 교과서 기술 아이콘 표시
- 책장·우클릭 메뉴·거래창에 설계도 연구 상태 표시, 거래 시 이미 보유한 설계도 중복 경고와 확인된 보유 책 제목 툴팁 제공
- **Ctrl + 좌클릭**으로 설정 횟수만큼 반복 독서, **Alt + 좌클릭**으로 직접 횟수 지정
- 기본 Read 및 Shift 동작 유지; 수동으로 지시한 독서의 Work 시간 지속 옵션 제공
- 바닐라 스타일 Basic / Icons 탭, 한국어·영어 UI
- **Quality Colors 호환성 수정:** 중복 설계도 툴팁에서 색상 태그는 제거하고 책 제목·품질 문구는 유지
- Release DLL의 개발자 전용 진단 코드 분리

## 현재 설치 가능 버전: 1.7

**`QoLEasyReading-v1.1.0-RW17-QCHF1.zip`**을 받아 `QoLEasyReading/` 폴더를 1.7 게임의 `Mods/` 폴더에 설치하세요. Harmony가 먼저 활성화돼 있어야 합니다. 이전 통합 모드 `ReadAndLearnQoL`은 동시에 켜지 마세요.

```text
Mods/QoLEasyReading/
  About/About.xml                # 현재 제공 ZIP에는 1.7만 지원 버전으로 표시
  1.7/Assemblies/QoLEasyReading.dll
  Languages/
  Textures/
```

**1.6 사용자 주의:** 이 설치 ZIP만으로는 1.6에서 작동하지 않습니다. 1.6 소스는 최신 QC 수정까지 반영됐지만, 지금까지 확인된 1.6 Release DLL은 수정 전 구형이어서 의도적으로 제외했습니다. [1.6 프로젝트](Source/1.6/QoLEasyReading)로 최신 Release를 빌드·검증한 다음 통합 ZIP을 다시 제작해야 합니다. **기존 1.6 DLL을 다시 넣으면 해결했던 오류가 재발할 수 있습니다.**

## 개발·검증

- [1.6 소스](Source/1.6/QoLEasyReading): .NET Framework 4.7.2 / Harmony 2
- [1.7 소스](Source/1.7/Projects/QoLEasyReading): .NET 10 / Harmony 3 / Waylith
- [빌드 안내](BUILDING.md) · [배포 체크리스트](RELEASE_CHECKLIST.md) · [배포 검증 현황](PACKAGING_STATUS.md)

QC 수정은 두 버전 Debug 실게임에서 확인했지만, **Release 실게임 확인까지 완료된 것은 아닙니다**. 이번 GitHub 패키징에서 독서·거래 C# 로직을 추가로 바꾸지 않았습니다.

## 제한 및 권리

Schematic 보유 판정은 알려진 저장 위치 중심으로 동작하며 타 모드의 모든 특수 보관함과 복합 거래행 방향을 완전히 보증하지 않습니다. `!`는 거래 차단이 아니라 안내입니다. `Schematic_Review.png`는 기존 경고 아이콘과 같은 예약용 자산입니다.

[MIT LICENSE](LICENSE)는 작성자가 권리를 보유한 부분에만 적용됩니다. 교과서 아이콘 13개와 Schematic 이미지의 재배포 권한은 공개 전 확인이 필요합니다.

© 2026 ZCN.
