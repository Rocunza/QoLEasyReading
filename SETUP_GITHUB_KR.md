# GitHub 첫 업로드 안내 (ZCN / Rocunza)

이 폴더는 두 통합 프로젝트의 압축이 아닌 **Easy Reading만 추출한 GitHub 업로드용 파일**입니다. Visual Studio 기존 솔루션에 덮어쓰지 마세요.

## 1. GitHub 빈 저장소 개설

https://github.com/new 접속 → Owner: `Rocunza`, Repository name: `QoLEasyReading`, Visibility: 처음은 `Private` 또는 바로 `Public`.

**Add a README / Add .gitignore / Choose a license는 전부 선택하지 마세요.** 이 ZIP에 세 파일이 이미 있습니다.

## 2. Git Bash에서 업로드

이 ZIP을 압축 해제한 뒤 **`README.md` 파일이 있는 `QoLEasyReading` 폴더**에서 Git Bash를 열고 실행합니다.

```bash
git init -b main
git add .
git commit -m "Initial release: QoL Easy Reading 1.6/1.7"
git remote add origin https://github.com/Rocunza/QoLEasyReading.git
git push -u origin main
```

`git commit`에서 이름/이메일 미설정 오류가 나면 Git Bash가 안내하는 `git config --global user.name` / `git config --global user.email`로 형님의 실제 GitHub 정보를 지정한 뒤 다시 실행하세요. GitHub 로그인/권한 인증 창이 뜨면 승인합니다. 비밀번호나 토큰을 코드나 채팅에 붙여넣지 마세요.

GitHub 주소가 다른 소유자나 이름으로 생성됐다면 remote URL도 실제 주소로 바꾸어야 합니다.

## 3. Steam ↔ GitHub 상호 링크

GitHub README의 Steam 링크는 기존 창작마당 ID `3828178548`로 설정했습니다.

스팀 창작마당 설명에 다음 문구를 추가합니다.

```text
◆ Source code / 소스 코드 (GitHub)
https://github.com/Rocunza/QoLEasyReading
```

필요하면 GitHub 저장소의 오른쪽 About → Website에도 Steam 주소를 설정하세요.

https://steamcommunity.com/sharedfiles/filedetails/?id=3828178548

## 4. 라이선스 확인

첨부 `LICENSE`는 **MIT License (Copyright (c) 2026 ZCN)** 입니다. 코드와 공식 아이콘 13개를 MIT 조건으로 배포할 권한이 있는지 최종 확인하세요. RimWorld 및 Harmony 라이브러리는 이 저장소에 포함되지 않으며 원 저작물의 라이선스를 그대로 따릅니다.

## 5. 유의할 차이

- 원본 Steam 패키지의 `About/preview.png`는 저장소에서 표준 파일명인 `About/Preview.png`로 이름만 변경했습니다. 이미지 바이트는 같습니다.
- `Mod/QoLEasyReading` 폴더의 1.6/1.7 Release DLL은 실제 첨부 Steam 배포본과 바이트 단위로 일치합니다.
- 개인 PC 경로가 들어간 `RimWorld.local.props` / `RimWorld17.local.props`는 공개하지 않았습니다. `.example` 파일만 포함합니다.
- `About.xml`의 `keeping vanilla reading behavior intact` 문구는 Work 시간표 예외 기본 ON을 감안하면 추후 문구 개선이 가능합니다. 현재 버전의 About.xml은 실제 Steam 배포본과 동일하게 보존했습니다.


## 6. GitHub Desktop에서 새 저장소 만들기 (추천)

1. GitHub Desktop에서 현재 `GrowthTiersPlus`가 선택되어 있더라도 **File → New repository...** 를 선택하면 기존 저장소는 변경되지 않습니다.
2. Name: `QoLEasyReading`, Description: `Book skill icons and repeat-reading QoL for RimWorld 1.6/1.7`, Local path: 원하는 로컬 상위 디렉터리를 입력하세요.
3. **Initialize this repository with a README** 체크 해제, Git ignore: **None**, License: **None**을 지정합니다. 제공된 압축파일에 README, .gitignore 및 MIT LICENSE가 이미 있기 때문입니다.
4. **Create repository** 클릭. 이 시점에서는 **로컬에만** 저장소가 생성됩니다.
5. ZIP에서 `QoLEasyReading/README.md`가 포함된 폴더 **안쪽 파일 전체**를 새로 생성된 로컬 `QoLEasyReading` 폴더에 복사합니다. `QoLEasyReading/QoLEasyReading/README.md` 형태로 중첩되지 않게 주의합니다.
6. GitHub Desktop의 **Changes**에서 의도한 파일만 있는지 확인합니다. 특히 `GrowthTiersPlus` 소스, `.vs`, `bin`, `obj`, 개인 경로 설정 및 토큰이 없어야 합니다.
7. Summary에 `Initial release: QoL Easy Reading (MIT)` 입력 → **Commit to main**.
8. 상단 **Publish repository** → 계정 확인 → 공개를 원하면 **Keep this code private** 체크 해제 → **Publish Repository**.
9. **Repository → View on GitHub**에서 실제 게시 결과를 확인합니다. Steam 소개글에 `https://github.com/Rocunza/QoLEasyReading`를 추가합니다.

기존 `GrowthTiersPlus`에서 **Repository → Publish** 또는 **Push**를 누르지 마세요. 언제나 상단 Current repository가 `QoLEasyReading`인지 확인하세요.
