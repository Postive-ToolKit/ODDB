# ODDB
## 개요
ODDB는 Unity에서 사용 가능한 반정형 오픈소스 데이터베이스 솔루션입니다.
테이블 에디터를 통해 생성한 Table을 특정 클래스와 바인드하여 손쉽게 접근하고 사용 가능하게 하는 것을 목표로 합니다.
생성된 테이블 데이터는 CSV 및 Google Sheets와 같은 외부 소스와 손쉽게 동기화할 수 있어 다양한 직군의 사람들이 협업하기에 용이합니다.

## 목차
- [주요 기능](#주요-기능)
- [ODDB Editor](#oddb-editor)
- [사용 방법](#사용-방법)
- [설치 방법](#설치-방법)

## 주요 기능
### 테이블 에디터
<img width="1041" height="637" alt="image" src="https://github.com/user-attachments/assets/8124d6aa-6860-4b76-bfb4-c3a101744cfe" />

ODDB는 Unity 에디터 내에서 테이블을 생성하고 편집할 수 있는 테이블 에디터를 제공합니다.
이를 통해 개발자는 별도의 데이터베이스 관리 도구 없이도 데이터를 쉽게 관리할 수 있습니다.
상단에 존재하는 메뉴에서 'ODDB/ODDB Editor'를 클릭해서 데이터베이스 수정 창을 열 수 있습니다.

### View 생성 기능
View는 ODDB에서 특정 테이블 군의 데이터의 형태를 정의하는 역할을 합니다.
생성한 View는 자신이 부모로 설정된 테이블들의 특성은 자신의 하위 특성으로 강제하는 기능을 가지고 있습니다.
이를 통해 개발자는 데이터베이스의 데이터를 객체 지향적으로 접근하고 조작할 수 았습니다.
- 예를 들어 `Item`이라는 테이블이 있고, `Weapon`과 `Armor`라는 두 개의 하위 테이블이 있다고 가정해봅시다.
  `Weapon`과 `Armor`는 모두 `Item`의 특성을 상속받습니다.
  이 경우, `Weapon`과 `Armor`는 각각 자신만의 고유한 특성(예: 공격력, 방어력)을 가지면서도 `Item`의 공통된 특성(예: 이름, 가격)을 공유할 수 있습니다.
- 또한 View를 상속받은 테이블은 자신과 Bind될 클래스의 스코프를 부모 View의 Bind 클래스와 그 하위 클래스로 제한받게 됩니다. 이와 같이 동작하는 목적은 실제 데이터 사용 시에도 객체 지향적인 접근 방식을 유지하기 위함입니다.

위의 예에서 설명한 Bind 클래스 기능과 관련된 내용을 아래에서 더 자세히 다루고 있습니다.

### Table 생성 기능
ODDB는 테이블 에디터를 통해 다양한 형태의 테이블을 생성할 수 있는 기능을 제공합니다.
개발자는 테이블 에디터에서 테이블의 이름, 열의 이름 및 데이터 타입 등을 정의할 수 있습니다.
또한 데이터를 손쉽게 추가/제거할 수 있습니다.
이렇게 생성된 테이블은 ODDB의 데이터베이스에 저장되며, 런타임 시점에 데이터를 로드하고 사용할 수 있습니다.

### Bind 클래스 기능
ODDB는 테이블과 특정 클래스를 바인드하여 데이터를 객체로 접근할 수 있는 기능을 제공합니다.
데이터 베이스 초기화 시 ODDB는 각 테이블의 데이터를 생성한 객체의 변수에 순서대로 할당 시도합니다.
이렇게 생성된 객체들은 `ODDBPort.GetEntity<T>(string id)` 메서드를 통해 ID로 접근할 수 있습니다.

이때 주의할 점은 Bind 클래스로 사용한 클래스는 기본적으로 `ODDBEntity` 클래스를 상속받아야 한다는 것입니다.
이는 ODDB가 데이터베이스 엔티티를 관리하고 조작하는 데 필요한 기본 기능을 제공하기 위함입니다.

### 외부 데이터 소스와의 동기화
ODDB는 CSV 파일 및 Google Sheets와 같은 외부 데이터 소스와의 동기화 기능을 제공합니다.
이를 통해 개발자는 다양한 직군의 사람들이 협업하여 데이터를 관리할 수 있습니다.
#### CSV 동기화
ODDB는 CSV 파일을 통해 테이블 데이터를 가져오고 내보낼 수 있는 기능을 제공합니다.
`ODDB/CSV/Import from CSV` 메뉴를 통해 CSV 파일에서 데이터를 불러올 수 있으며, `ODDB/CSV/Export to CSV` 메뉴를 통해 현재 테이블 데이터를 CSV 파일로 내보낼 수 있습니다.

출력된 테이블은 `{테이블 이름}_{테이블 ID}` 파일명으로 저장되며 EXCEL 등 CSV를 지원하는 프로그램에서 열어서 편집할 수 있습니다.

#### Google Sheets 동기화
ODDB는 Google Sheets와의 연동 기능도 제공합니다. CSV보다 더 직관적이고 유용한 기능들을 데이터 관리에 활용할 수 있습니다.
단 ODDB를 Google Sheets와 연동하기 위해서 Google Sheets의 `Apps Script` 사용해야 합니다.
자세한 Google Sheets 연동 방법은 [Google Sheets 연동 가이드](./GoogleSheetGuides/GoogleSheetsGuide_KR.md)를 참고하시기 바랍니다.

연동이 완료되었다면 `ODDB/Google Sheets/Import from Google Sheets` 메뉴를 통해 Google Sheets에서 데이터를 불러올 수 있으며, `ODDB/Google Sheets/Export to Google Sheets` 메뉴를 통해 현재 테이블 데이터를 Google Sheets로 내보낼 수 있습니다.

## ODDB Editor
<img width="1045" height="654" alt="image" src="https://github.com/user-attachments/assets/0ee9385b-1b9d-4afc-b05e-26cbfcbc5ee8" />

`ODDB Editor`는 ODDB의 핵심 기능 중 하나로, Unity 에디터 내에서 테이블을 생성하고 편집할 수 있는 인터페이스를 제공합니다.
`ODDB Editor`를 통해 개발자는 별도의 데이터베이스 관리 도구 없이도 데이터를 쉽게 관리할 수 있습니다.
### Table 생성
에디터를 실행한 후 좌측의 리스트에 `우클릭/Add/Table`을 선택하여 새로운 테이블을 생성할 수 있습니다.
생성된 테이블은 기본적으로 `Default Name`이라는 이름을 가지며 기본적으로 아무런 View에도 속하지 않습니다.

테이블의 이름을 변경하려면 리스트에서 해당 테이블을 선택한 후 우측에 생성되는 에디터의 상단 텍스트 박스에서 수정할 수 있습니다.
### View 생성
에디터를 실행한 후 좌측의 리스트에 `우클릭/Add/View`를 선택하여 새로운 View를 생성할 수 있습니다.
생성된 View는 기본적으로 `Default Name`이라는 이름을 가지며, 기본적으로 아무런 View에도 속하지 않습니다.

마찬가지로 테이블의 이름을 변경하려면 리스트에서 해당 View를 선택한 후 우측에 생성되는 에디터의 상단 텍스트 박스에서 수정할 수 있습니다.

### 공용 메뉴
<img width="792" height="106" alt="image" src="https://github.com/user-attachments/assets/1ea356cf-b20b-4549-93c5-62361a9f6549" />

생성된 Table 또는 View를 선택하면 우측에 해당 Table/View를 수정할 수 있는 에디터가 나타납니다.
해당 에디터 상단에서 기본적으로 공용으로 사용되는 몇 가지 설정을 할 수 있습니다.
#### 이름 변경
에디터 상단의 텍스트 박스에서 현재 선택된 Table/View의 이름을 변경할 수 있습니다.
이름은 고유하지 않아도 되며 나중에 언제든지 변경할 수 있습니다.
이후 해당 테이블을 데이터로 사용하는데에도 영향을 주지 않으며 단지 식별 목적으로만 사용됩니다.

#### View/Table 에디터 변경 기능
에디터 상단의 `Table/View` 버튼을 클릭하여 현재 보고있는 Table 또는 View의 설정 에디터로 전환할 수 있습니다.
`View`는 기본적으로 `View`로, `Table`은 기본적으로 `Table`로 표시되어 있습니다.
각각의 테이블 타입에 따라 사용할 수 있는 에디터는 다음과 같습니다.
- `View` : `View` 설정 에디터
- `Table` : `View` 설정 에디터, `Table` 설정 에디터

각각의 설정 에디터들은 아래와 같은 기능들을 제공합니다.
- `View` 설정 에디터 : View의 필드 설정을 수정할 수 있습니다.
- `Table` 설정 에디터 : Table의 Row 데이터를 수정할 수 있습니다.

#### Bind 클래스 설정
<img width="1036" height="357" alt="image" src="https://github.com/user-attachments/assets/6bfb7f8e-eb36-45e1-ba1c-9c354a5741f5" />

기본적으로 에디터 상단 툴바에 `Bind:None`로 표시된 드롭다운 메뉴가 있습니다.
해당 드롭다운 메뉴를 클릭하면 현재 선택된 Table/View에 바인드할 수 있는 클래스들의 리스트가 나타납니다.
리스트에서 원하는 클래스를 선택하면 해당 Table/View에 바인드할 클래스가 설정됩니다.
바인드된 클래스는 이후 런타임 시점에 해당 Table/View의 데이터를 객체로 변환하는 데 사용됩니다.

만약 `None`을 선택하면 해당 Table/View에는 바인드된 클래스가 없음을 의미합니다.
이와 같이 설정될 경우 런타임 시점에 해당 Table/View의 데이터 사용하지 않는 것과 동일하게 동작합니다.

#### Inherit View 설정
<img width="1042" height="377" alt="image" src="https://github.com/user-attachments/assets/f658caf2-621c-4f23-ba2c-fc6b44a292a1" />

마찬가지로 에디터 상단 툴바에 `Inherit:None`로 표시된 드롭다운 메뉴가 있습니다.
해당 드롭다운 메뉴를 클릭하면 현재 선택된 `Table/View`가 상속할 수 있는 View들의 리스트가 나타납니다.
리스트에서 원하는 View를 선택하면 해당 `Table/View`가 선택된 View를 상속하게 됩니다.
상속된 View는 이후 해당 `Table/View`의 필드 설정에 영향을 미치게 됩니다. 이러한 영향은 즉각적으로 반영됩니다.

만약 `None`을 선택하면 해당 Table/View는 어떤 View도 상속하지 않음을 의미합니다.

### `View` 설정 에디터
<img width="794" height="274" alt="image" src="https://github.com/user-attachments/assets/01c720ac-a7c1-4b6d-b083-f1bc620b78cb" />


`View` 설정 에디터는 View와 Table 모두에서 사용 가능한 에디터입니다.
기본적으로 모든 Table들은 View를 상속받기 때문에 동일한 방식으로 필드 데이터를 수정하는 것이 가능합니다.

우측에 나타난 에디터의 상단을 보면 `Table/View`로 표시되어 있는 버튼이 있습니다. View는 기본적으로 `View`로, Table은 `Table`로 표시되어 있습니다.
해당 버튼에서 `View`를 선택하면 현재 보고있는 `Table/View`의 Field들을 수정할 수 있는 에디터가 나타납니다.

이때 상위 View에서 변경된 Field 설정은 하위 Table/View에 즉각적으로 반영됩니다.
하지만 하위 Table/View에서 변경된 Field 설정은 상위 View에 영향을 미치지 않으며 자기 자신에게만 영향을 미칩니다

모든 Field 변경은 Scope 단위로 관리됩니다.
- 상위 View에서 추가된 Field는 하위 Table/View에 모두 동일하게 추가됩니다.
- 하위 Table/View에서 추가된 Field는 상위 View에 영향을 미치지 않으며 자기 자신에게만 추가됩니다.

즉 하위 Table/View은 상위 View의 Field들을 모두 상속받으며, 추가적으로 자기 자신만의 Field들을 가질 수 있습니다.

#### Field 추가
Field를 추가하려면 `Add Field` 버튼을 클릭합니다.
새로운 Field가 추가되면 기본적으로 `Default Field`라는 이름과 `string` 타입을 가지게 됩니다.
필드의 이름과 타입은 각각의 텍스트 박스와 드롭다운 메뉴를 통해 수정할 수 있습니다.
필드의 타입은 `string`, `int`, `float`, `bool` 등 다양한 데이터 타입 중에서 선택할 수 있습니다.
또한 유니티에서 제공하는 몇몇 `Asset` 타입들도 지원합니다.

#### Field 제거
필드를 제거하려면 해당 필드의 우측에 있는 `-` 버튼을 클릭합니다.
필드가 제거되면 해당 필드에 저장된 모든 데이터도 함께 삭제됩니다.

### `Table` 설정 에디터
<img width="788" height="341" alt="image" src="https://github.com/user-attachments/assets/5119eaf4-ae64-4dbc-9a5a-5be11ae0da82" />
`Table` 설정 에디터는 Table만 사용 가능한 에디터입니다.
Table을 선택한 상태에서 우측 에디터 상단의 `Table` 버튼을 클릭하면 Table 설정 에디터가 나타납니다.
기본적으로 Table로 설정되어 있습니다.

#### Row 추가
`Create Row` 버튼을 클릭하여 새로운 Row를 추가할 수 있습니다.
새로운 Row가 추가되면 상위 View에서 정의된 모든 Field들이 기본적으로 포함됩니다.
각 Row의 Field 데이터는 해당 Row의 텍스트 박스와 드롭다운 메뉴를 통해 수정할 수 있습니다.


#### Row 제거
Row를 제거하려면 해당 Row의 우측에 있는 `-` 버튼을 클릭합니다.
Row가 제거되면 해당 Row에 저장된 모든 데이터도 함께 삭제됩니다.

## 사용 방법
ODDB는 게임이 런타임에 진입할 때 데이터베이스를 초기화합니다.
때문에 별도로 초기화 코드를 작성할 필요가 없습니다.
데이터베이스가 초기화된 후에는 `ODDBPort` 클래스를 통해 데이터베이스에 접근할 수 있습니다.

### 데이터 접근
ODDBPort 클래스는 데이터베이스에 접근하기 위한 다양한 메서드를 제공합니다.

#### GetEntity 메서드
가장 기본적인 메서드는 `GetEntity<T>(string id)` 메서드로, 특정 ID를 가진 데이터를 객체로 반환합니다.
- 예를 들어, `Item` 테이블이 `ItemData` 클래스와 바인드되어 있다고 가정해봅시다.
  이 경우, `Item` 테이블의 각 행은 `ItemData` 클래스의 인스턴스로 변환됩니다.
  개발자는 `ODDBPort.GetEntity<ItemData>("item_001")`와 같은 방법으로 특정 아이템 데이터를 객체로 쉽게 접근할 수 있습니다

#### GetEntities 메서드
또 다른 유용한 메서드는 `GetEntities<T>()` 메서드로, 특정 클래스와 바인드된 모든 데이터를 객체의 리스트로 반환합니다.
- 예를 들어 `Weapon` 테이블이 `WeaponData` 클래스와 바인드되어 있다고 가정해봅시다.
  이 경우, 개발자는 `ODDBPort.GetEntities<WeaponData>()`와 같은 방법으로 모든 무기 데이터를 객체의 리스트로 쉽게 접근할 수 있습니다.
- 두번째 예로 `Item` View에 바인드된 `ItemData` 클래스가 있으며 해당 View를 상속받는 `Weapon`과 `Armor` 테이블이 있다고 가정해봅시다.
  이 경우, 개발자는 `ODDBPort.GetEntities<ItemData>()`와 같은 방법으로 `Weapon`과 `Armor` 테이블의 모든 데이터를 포함하는 객체의 리스트로 쉽게 접근할 수 있습니다.

---
### 설치 방법
<img width="617" height="153" alt="image" src="https://github.com/user-attachments/assets/de3c0af9-9a4b-445f-883d-de76d1362388" />

ODDB는 Unity 패키지 매니저를 통해 설치할 수 있습니다.
1. Unity 에디터를 열고, 상단 메뉴에서 `Window > Package Manager`를 선택합니다.
2. 패키지 매니저 창이 열리면, 좌측 상단의 `+` 버튼을 클릭하고, `Add package from git URL...`을 선택합니다.
3. 팝업 창에 `https://github.com/Postive-ToolKit/ODDB.git`를 입력하고 `Add` 버튼을 클릭합니다.
