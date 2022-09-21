using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace MyBusinessNamespace {

  public interface IFooService {

    bool Foooo(string a, out int b);

    TestModel Kkkkkk(int optParamA = 0, string optParamB = "f" );

    /// <summary>
    /// Meth
    /// </summary>
    /// <param name="errorCode"> Bbbbbb </param>
    void AVoid(TestModel errorCode);

    bool TestNullableDt(DateTime? dt);

  }

  /// <summary>
  /// MMMMMMMMMMMMMMMMMMM
  /// </summary>
  public class TestModel {

    /// <summary>
    /// jfjfj
    /// </summary>
    [Required()]
    public String FooBar { get; set; } = "default";

    public String OptionalProp { get; set; } = "default";

    [Required()]
    public MyEnum MyEnumProp{ get; set; } = MyEnum.Bar;

  }

  public enum MyEnum {
    Foo = 1,
    /// <summary> this is the Magic of BAR! </summary>
    Bar = 2,
    Baz = 1
  }

}
