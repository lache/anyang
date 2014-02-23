#ifndef __EXCLUSIVE_RUN_H__
#define __EXCLUSIVE_RUN_H__

class exclusive_run_t
{
public:
    typedef std::atomic_flag flag_t;
    typedef flag_t* flag_ptr_t;

public:
    bool is_acquired() const;

    exclusive_run_t(flag_ptr_t);
    ~exclusive_run_t();
    
    static void init(flag_ptr_t flag);

private:
    flag_ptr_t  flag;
    bool        acquired;
};

inline exclusive_run_t::exclusive_run_t(flag_ptr_t _flag)
    : flag(_flag), acquired(false)
{
    acquired = flag->test_and_set() == false;
}

inline exclusive_run_t::~exclusive_run_t()
{
    if (is_acquired())
        flag->clear();
}

inline bool exclusive_run_t::is_acquired() const
{
    return acquired;
}

inline void exclusive_run_t::init(flag_ptr_t flag)
{
    flag->clear();
}

#endif