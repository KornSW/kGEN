using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Reflection;

namespace UnitTests {

  [TestClass]
  public class FrameworkTests {

    [TestMethod]
    public void TestInOutRef() {

      Type mockClass1Type = typeof(MockClass1);
      MethodInfo method =  mockClass1Type.GetMethod(nameof(MockClass1.MethodWithInOutRefParams));

      ParameterInfo[] parameters = method.GetParameters();

      ParameterInfo inParam = parameters[0];  //weder isOut noch isRef
      Assert.IsFalse(inParam.IsOut);
      //Assert.IsTrue(inParam.IsIn); // es scheint dass das seit neustem immer false ist
      Assert.IsFalse(inParam.ParameterType.IsByRef);
      Assert.IsFalse(inParam.ParameterType.IsByRefLike);
      Assert.IsFalse(inParam.IsOptional);

      ParameterInfo refParam = parameters[1];    //nur isRef
      Assert.IsFalse(refParam.IsOut); //war das nicht früher auch true?
      //Assert.IsTrue(refParam.IsIn); // es scheint dass das seit neustem immer false ist
      Assert.IsTrue(refParam.ParameterType.IsByRef);
      Assert.IsFalse(refParam.ParameterType.IsByRefLike);
      Assert.IsFalse(refParam.IsOptional);

      ParameterInfo outParam = parameters[2];  // isOut + isRef
      Assert.IsTrue(outParam.IsOut);
      //Assert.IsFalse(outParam.IsIn);
      Assert.IsTrue(outParam.ParameterType.IsByRef);
      Assert.IsFalse(outParam.ParameterType.IsByRefLike);
      Assert.IsFalse(outParam.IsOptional);

      ParameterInfo optInParam = parameters[3];
      Assert.IsFalse(optInParam.IsOut);
      //Assert.IsTrue(optInParam.IsIn);
      Assert.IsFalse(optInParam.ParameterType.IsByRef);
      Assert.IsFalse(optInParam.ParameterType.IsByRefLike);
      Assert.IsTrue(optInParam.IsOptional);

    }

  }

  internal class MockClass1 {

    public string MethodWithInOutRefParams(
      string inParam, ref string refParam, out string outParam, string optInParam = "default"
    ) {
      outParam = "";
      return "";
    }



  }

}
