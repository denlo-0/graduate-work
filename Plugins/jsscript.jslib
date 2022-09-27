mergeInto(LibraryManager.library, {
	getToken: function getToken()
	{
		var returnStr = window.sessionStorage.getItem("token");
		console.log(returnStr);
		var bufferSize = lengthBytesUTF8(returnStr) + 1;
		var buffer = _malloc(bufferSize);
		stringToUTF8(returnStr, buffer, bufferSize);
		return buffer;
	}
});