#pragma once

#include "msg_reader.h"
#include "msg_writer.h"
#include "msg_session_ref.h"
#include "exclusive_run.h"

using asio::ip::tcp;

class msg_session
        : public std::enable_shared_from_this<msg_session> {
public:
    msg_session(asio::io_service& io_service);
    virtual ~msg_session();

    // control connectiviy
    void connect(tcp::resolver::iterator endpoint_iterator);
    void close();            //< close right now!

    // generic message writer
    template <typename _MsgTy>
    void write(const _MsgTy& msg);

    // io-request
    void request_read_msg_size();
    void request_read_msg();
    void request_write();

    // status
    bool is_connected() const;

    // process accepted event
    void accepted();

protected:
    // io-handler
    virtual void handle_connect(const std::error_code& error, tcp::resolver::iterator endpoint_iterator);
    virtual void handle_read_msg_size(const std::error_code& error);
    virtual void handle_read_msg(const std::error_code& error);
    virtual void handle_write(const std::error_code& error);

    // error-handler
    virtual void handle_error(const std::error_code& error);

    // msg dispatcher
    virtual void dispatch_msg(msg_type_t msg_type, msg_reader reader);

private:
    void do_write(msg_writer_ref writer);
    void do_close();
    
public:
    // network entitys
    tcp::socket socket;
    asio::io_service& io_service;

protected:
    // message receive buffer
    msg_size_t msg_size;
    std::size_t msg_buffer_length;
    msg::byte* msg_buffer;

private:
    // message queue for concurrency
    #ifdef WIN32
    typedef Concurrency::concurrent_queue<msg_writer_ref> write_msgs_t;
    #else
    typedef tbb::concurrent_queue<msg_writer_ref> write_msgs_t;
    #endif
    write_msgs_t write_msgs;

    // to close after current io completion
    bool request_close;

    // is socket connected?
    bool connected;

    // exclusive_run flag in do_write
    exclusive_run_t::flag_t write_execlusion;
};


template <typename _MsgTy>
inline void msg_session::write(const _MsgTy& msg)
{
    msg_writer_ref writer(new msg_writer);
    (*writer) << msg;
    
    io_service.post(std::bind(&msg_session::do_write, shared_from_this(), writer));
}

// test session for queueing all of received messages
class test_msg_session : public msg_session
{
public:
    typedef std::pair<msg_type_t, msg_reader> msg_pair;

    test_msg_session(asio::io_service& io_service);
    virtual ~test_msg_session();

    bool is_queue_empty();
    msg_pair peek_msg();
    void pop_msg();
    void clear_msg();

protected:
    // msg dispatcher
    virtual void dispatch_msg(msg_type_t msg_type, msg_reader reader);

private:
    typedef std::deque<msg_pair> msg_queue_t;
    msg_queue_t msg_queue;

    typedef std::recursive_mutex mutex_type;
    typedef std::lock_guard<mutex_type> lock_type;

    mutex_type mutex;
};
