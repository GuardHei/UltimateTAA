using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialManager {

	public static Material ErrorMat {
		get {
			if (_errorMat == null) _errorMat = new Material(Shader.Find("Hidden/InternalErrorShader"));
			return _errorMat;
		}
	}

	private static Material _errorMat;
}