
i18n = require "Common/i18n"

function main()
	print(i18n["1004"])

	-- 实例化预制
	local canvas = UnityEngine.GameObject.Find("Canvas").transform
	local obj = UnityEngine.Object.Instantiate(UnityEngine.Resources.Load("Prefab/my_lab"))
	obj.transform:SetParent(canvas)
	obj.transform.localScale = Vector3(1,1,1)
	obj.transform.localPosition = Vector3.zero
end

main()
