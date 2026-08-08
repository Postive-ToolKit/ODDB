# Google Sheets API 연동 가이드

ODDB는 Google OAuth 사용자 인증과 Google Sheets API v4를 사용해 Google Sheets를 가져오고 내보냅니다. Apps Script 배포나 서비스 계정 공유는 필요하지 않습니다.

## 1. Google OAuth Client 생성

1. [Google Cloud Console](https://console.cloud.google.com/)에서 프로젝트를 생성하거나 선택합니다.
2. `API 및 서비스 > 라이브러리`에서 `Google Sheets API`를 활성화합니다.
3. OAuth 동의 화면과 대상 사용자를 구성합니다.
4. 애플리케이션 유형이 `데스크톱 앱`인 OAuth 2.0 Client를 생성합니다.
5. 생성된 Client ID와 Client Secret을 복사합니다.

## 2. Spreadsheet 준비

1. 대상 Google Spreadsheet를 엽니다.
2. `공유`를 누릅니다.
3. Unity에서 인증할 Google 계정에 편집자 권한이 있는지 확인합니다.
4. Spreadsheet URL에서 `/d/`와 `/edit` 사이의 Spreadsheet ID를 복사합니다.

## 3. ODDB 설정

1. `ODDBEditorSettings` 에셋을 선택합니다.
2. `Google Sheets Authentication`에 Desktop OAuth Client ID와 Client Secret을 입력하고 `Save OAuth Client`를 누릅니다.
3. `Google Sheets Settings`에서 `Google Spreadsheet Id`를 입력합니다.
4. `Sheet Import / Export`에서 출력 레이아웃을 선택합니다.
   - `PerTable`: 기존처럼 Table마다 별도 탭/CSV를 사용합니다.
   - `GroupByRootView`: 최상위 부모 View의 모든 하위 Table을 하나의 탭/CSV에 블록으로 묶습니다.
5. `Sign in with Google`을 누르고 브라우저 인증을 완료합니다.
6. `Test Connection`으로 인증과 Spreadsheet 접근 권한을 확인합니다.

설정이 완료되지 않은 상태에서 Import 또는 Export를 실행하면 ODDB가 안내창을 표시하고 Inspector에서 `ODDBEditorSettings`를 선택합니다.

OAuth Client 자격 증명은 암호화된 뒤 `ODDBEditorSettings`에 직렬화됩니다. 이는 평문 노출을 방지하지만 ODDB에도 복호화 로직이 포함되므로 저장소 접근 제어를 대체하지는 않습니다. 자격 증명이 들어간 프로젝트 저장소는 Private으로 유지하세요. OAuth 토큰은 사용자 로컬 애플리케이션 데이터 폴더에 저장되며 프로젝트 에셋에는 기록되지 않습니다.

## 4. 동기화

- ODDB Editor의 Import/Export 메뉴에서 `Google Sheets`를 선택합니다.
- 최초 Export 시 기존 Apps Script 형식의 탭을 감지해 ODDB metadata를 추가합니다.
- Export로 연결된 물리 시트 키(Table ID 또는 Root View ID)와 Google Sheet의 숫자 `sheetId`는 Spreadsheet ID별로 `UserSettings/ODDBGoogleSheetsBindings.json`에 저장됩니다. 이 파일은 프로젝트 로컬 설정이며 에셋이나 OAuth 자격 증명을 포함하지 않습니다.
- 로컬 바인딩이 없거나 손상되면 기존 Google Sheet의 ODDB metadata에서 자동 복구하므로 기존 탭이 중복 생성되지 않습니다. 사용자가 탭 이름을 변경해도 동일한 `sheetId` 연결이 유지됩니다.
- ODDB에서 제거된 관리 컬럼은 다음 Export에서 실제 Google Sheet 컬럼으로 삭제됩니다.
- 삭제 전 확인 창이 표시되며, 사용자 관리 컬럼과 `#` 주석 행은 보존됩니다.
- 새 Google 탭은 Google Sheets 기본값인 1,000행 × 26열 대신 실제 Export 데이터에 필요한 행과 열 크기로 생성됩니다. 기존 탭은 공간이 부족할 때만 확장합니다.
- `#NAME` 헤더가 `#`으로 시작하는 컬럼(예: `#Designer Notes`)은 사용자 관리 컬럼입니다. ODDB는 해당 물리 컬럼에 아무 값도 쓰지 않고 다음 사용 가능한 컬럼에 관리 필드를 기록하며, 재사용할 공간이 없을 때만 새 컬럼을 추가합니다.
- ODDB에서 제거된 행은 다음 Export에서 실제 Google Sheet 행으로 삭제됩니다. 기존 `#REMOVED` 행도 같은 규칙으로 정리됩니다.
- 행 전체가 삭제되므로 해당 행의 사용자 관리 컬럼 값도 함께 삭제됩니다. 필요한 경우 Google Sheets 버전 기록에서 복구할 수 있습니다.

필드명은 컬럼 식별자로 사용됩니다. 필드명 변경은 기존 컬럼 삭제와 새 컬럼 추가로 처리되므로 해당 컬럼의 수동 서식은 유지되지 않을 수 있습니다.

### GroupByRootView 레이아웃

- `ItemView` 아래의 `WeaponItem`, `CurrencyItem` 같은 Table은 `ItemView` 탭 하나에 `#TABLE`/`#END_TABLE` 블록으로 기록됩니다.
- 각 블록은 독립적인 `#NAME`과 `#TYPE` 행을 가지므로 서로 다른 필드 구성을 사용할 수 있습니다.
- Google Sheets의 View 참조 타입은 `View-ItemData`처럼 읽기 쉬운 View 이름으로 표시되고, 안정적인 연결 ID는 해당 타입 셀의 `ODDB View ID: ...` 노트에 저장됩니다. View 이름이 바뀌어도 노트 ID로 연결을 복구하며, 노트가 없으면 표시된 이름으로 가져옵니다. CSV도 같은 표시 이름을 사용하고 각 `#TYPE` 바로 아래의 `#VIEW_ID` 행에 안정적인 ID를 저장합니다.
- Google Sheets에서는 `#ODDB_GROUP`/`#END_GROUP` 행을 하늘색, `#TABLE`/`#NAME`/`#TYPE` 행을 회색, `#END_TABLE` 행을 부드러운 붉은색으로 표시하며 모두 흰색 굵은 글자를 사용합니다. 서식은 ODDB 관리 컬럼 범위에만 적용됩니다.
- 선택한 Table만 Export해도 같은 Root View의 모든 형제 Table을 함께 내보내 기존 블록 손실을 방지합니다.
- View를 선택하면 모든 하위 Table을 대상으로 Export/Import Selected를 사용할 수 있습니다. 중첩 View도 재귀적으로 탐색하며, 그룹 Export는 안전한 물리 단위인 Root View 탭 전체를 갱신합니다.
- Table의 부모 View가 바뀌면 새 그룹에 추가하고 이전 ODDB 그룹에서는 해당 블록을 제거합니다.
- 기존 Table별 탭과 그룹 탭이 동시에 존재하면 현재 `Sheet Layout Mode`에 맞는 데이터를 우선 사용합니다.
- 그룹 밖에 사용자가 추가한 행과 사용자 컬럼은 물론, 관리 컬럼 사이에 명시적으로 추가한 주석 컬럼도 보존됩니다.
