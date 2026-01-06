using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace day02
{
    // dao : databases access object. db 처리 구현하는 클래스
    interface Dao
    {
        void insert();
        void delete();
        void select();
        void update();
    }

    // Dao 인터페이스를 oracle에 적합하게 구현
    class OracleDao : Dao
    {
        public void insert()
        {
            Console.WriteLine("oracle insert");
        }
        public void delete()
        {
            Console.WriteLine("oracle delete");
        }
        public void select()
        {
            Console.WriteLine("oracle select");
        }
        public void update()
        {
            Console.WriteLine("oracle update");
        }
    }

    class MysqlDaoImpI : Dao
    {
        public void insert()
        {
            Console.WriteLine("Mysql insert");
        }
        public void delete()
        {
            Console.WriteLine("Mysql delete");
        }
        public void select()
        {
            Console.WriteLine("Mysql select");
        }
        public void update()
        {
            Console.WriteLine("Mysql update");
        }
    }

    class Service
    {
        // db작업을 위한 Dao 객체
        // 타입을 인터페이스로 하여 이를 상속보다는 구현 클래스는 모두 할당 가능
        private Dao dao;

        // dao 인터페이스 객체를 업캐스팅해서 받아와서 
        //         public Service(Dao dao) 이 부분을 통해서 의존성이 주입이 되고, 컴포넌트가 연결되는 부분임

        public Service(Dao dao) // 그럼 얘를 어떻게 변신을 하냐?
        {
            this.dao = dao; // 이렇게 써야지 "외부에서" 누가 들어올지 결정해줌 (의존성 주입 - DI)
            // this.dao = new OracleDao -? 결합도가 매우 높은 것 -> 다른 회사의 DB도 쓸 수 있게 해야 하는데
        }
        public void add()
        {
            dao.insert();
        }
        public void get()
        {
            dao.select();
        }
        public void edit()
        {
            dao.update();
        }
        public void del()
        {
            dao.delete();
        }


    }
}
